using CFIT.AppLogger;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pmdg737Interface
{
    public enum Pmdg737DoorId
    {
        FwdL,
        FwdR,
        AftL,
        AftR,
        OverwingL,
        OverwingR,
        CargoFwd,
        CargoAft,
        CargoMain,
        Equipment,
        Airstair,
    }

    public class Pmdg737Door(
        Pmdg737Aircraft aircraft,
        Pmdg737DoorId id,
        int eventCode,
        string progressVariable = "")
    {
        protected const double ClosedThreshold = 5.0;
        protected const double OpenThreshold = 90.0;

        public virtual Pmdg737Aircraft Aircraft { get; } = aircraft;
        public virtual Pmdg737DoorId Id { get; } = id;
        public virtual int EventCode { get; } = eventCode;
        public virtual string ProgressVariable { get; } = progressVariable ?? "";

        protected virtual SemaphoreSlim OperationLock { get; } = new(1, 1);

        public virtual bool IsOpenIndicated => Id switch
        {
            Pmdg737DoorId.FwdL => Aircraft.Data.DOOR_annunFWD_ENTRY != 0,
            Pmdg737DoorId.FwdR => Aircraft.Data.DOOR_annunFWD_SERVICE != 0,
            Pmdg737DoorId.AftL => Aircraft.Data.DOOR_annunAFT_ENTRY != 0,
            Pmdg737DoorId.AftR => Aircraft.Data.DOOR_annunAFT_SERVICE != 0,
            Pmdg737DoorId.OverwingL =>
                Aircraft.Data.DOOR_annunLEFT_FWD_OVERWING != 0 ||
                Aircraft.Data.DOOR_annunLEFT_AFT_OVERWING != 0,
            Pmdg737DoorId.OverwingR =>
                Aircraft.Data.DOOR_annunRIGHT_FWD_OVERWING != 0 ||
                Aircraft.Data.DOOR_annunRIGHT_AFT_OVERWING != 0,
            Pmdg737DoorId.CargoFwd => Aircraft.Data.DOOR_annunFWD_CARGO != 0,
            Pmdg737DoorId.CargoAft => Aircraft.Data.DOOR_annunAFT_CARGO != 0,
            Pmdg737DoorId.Equipment => Aircraft.Data.DOOR_annunEQUIP != 0,
            Pmdg737DoorId.Airstair => Aircraft.Data.DOOR_annunAIRSTAIR != 0,
            _ => false,
        };

        public virtual bool HasProgressVariable => !string.IsNullOrWhiteSpace(ProgressVariable);

        public virtual double Progress
        {
            get
            {
                if (!HasProgressVariable)
                    return IsOpenIndicated ? 100.0 : 0.0;

                try
                {
                    return Aircraft.SimStore[ProgressVariable]?.GetNumber()
                        ?? (IsOpenIndicated ? 100.0 : 0.0);
                }
                catch
                {
                    return IsOpenIndicated ? 100.0 : 0.0;
                }
            }
        }

        public virtual bool IsMoving
        {
            get
            {
                if (!HasProgressVariable)
                    return false;

                double progress = Progress;
                return (progress > ClosedThreshold && progress < OpenThreshold)
                    || (progress <= ClosedThreshold && IsOpenIndicated);
            }
        }

        public virtual bool IsMostlyOpen
        {
            get
            {
                if (!HasProgressVariable)
                    return IsOpenIndicated;

                return Progress >= OpenThreshold;
            }
        }

        public virtual bool IsMostlyClosed
        {
            get
            {
                if (!HasProgressVariable)
                    return !IsOpenIndicated;

                return Progress <= ClosedThreshold && !IsOpenIndicated;
            }
        }

        public virtual async Task<bool> SetOpen(bool targetOpen)
        {
            if (!Aircraft.ExperimentalWritesEnabled || !Aircraft.DoorAutomationEnabled)
            {
                if (Aircraft.StateLoggingEnabled)
                    Logger.Debug($"PMDG 737 write suppressed: {Id} targetOpen={targetOpen}");
                return false;
            }

            try
            {
                await OperationLock.WaitAsync(Aircraft.Token);
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            try
            {
                if (TargetReached(targetOpen))
                    return true;

                if (IsMoving)
                {
                    await WaitForSettled(7000);
                    if (TargetReached(targetOpen))
                        return true;
                }

                await SendDoorCode();

                bool reached = await WaitForTarget(
                    targetOpen,
                    Id == Pmdg737DoorId.Airstair ? 14000 : 10000);

                if (!reached)
                {
                    Logger.Warning(
                        $"PMDG 737 door target timeout: {Id} targetOpen={targetOpen} " +
                        $"progress={Progress:0.0} annunciator={IsOpenIndicated}");
                }

                return reached;
            }
            finally
            {
                OperationLock.Release();
            }
        }

        protected virtual bool TargetReached(bool targetOpen)
        {
            return targetOpen ? IsMostlyOpen : IsMostlyClosed;
        }

        protected virtual async Task WaitForSettled(int timeoutMs)
        {
            int elapsed = 0;

            while (IsMoving
                && elapsed < timeoutMs
                && !Aircraft.Token.IsCancellationRequested)
            {
                await Task.Delay(150, Aircraft.Token);
                elapsed += 150;
            }
        }

        protected virtual async Task<bool> WaitForTarget(
            bool targetOpen,
            int timeoutMs)
        {
            int elapsed = 0;

            // Give the custom PMDG event a short moment to start changing state.
            await Task.Delay(200, Aircraft.Token);
            elapsed += 200;

            while (elapsed < timeoutMs
                && !Aircraft.Token.IsCancellationRequested)
            {
                if (TargetReached(targetOpen))
                    return true;

                await Task.Delay(150, Aircraft.Token);
                elapsed += 150;
            }

            return TargetReached(targetOpen);
        }

        public virtual Task Toggle()
        {
            if (!Aircraft.ExperimentalWritesEnabled
                || !Aircraft.DoorAutomationEnabled)
            {
                return Task.CompletedTask;
            }

            return SendDoorCode();
        }

        protected virtual Task SendDoorCode()
        {
            string evt = Pmdg737Sdk.GetEventName(EventCode);

            Logger.Information(
                $"PMDG 737 native door event: {Id} -> {Pmdg737Sdk.GetEventId(EventCode)}");

            return Aircraft.SimStore[evt]?.WriteValue(Pmdg737Sdk.MouseLeftSingle)
                ?? Task.CompletedTask;
        }
    }
}

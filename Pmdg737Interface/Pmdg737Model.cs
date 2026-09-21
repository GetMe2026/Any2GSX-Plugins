namespace Pmdg737Interface
{
    public static class Pmdg737Model
    {
        public static string GetName(ushort model)
        {
            return model switch
            {
                1 => "737-600",
                2 => "737-700",
                3 => "737-700 BW",
                4 => "737-700 SSW",
                5 => "737-800",
                6 => "737-800 BW",
                7 => "737-800 SSW",
                8 => "737-900",
                9 => "737-900 BW",
                10 => "737-900 SSW",
                11 => "737-900ER BW",
                12 => "737-900ER SSW",
                13 => "737-700 BDSF BW",
                14 => "737-700 BDSF SSW",
                15 => "737-800 BDSF BW",
                16 => "737-800 BDSF SSW",
                17 => "737-800 BCF BW",
                18 => "737-800 BCF SSW",
                19 => "737-700 BBJ BW",
                20 => "737-700 BBJ SSW",
                21 => "737-800 BBJ BW",
                _ => $"737 NG model {model}",
            };
        }

        public static bool IsCargo(ushort model)
        {
            return model >= 13 && model <= 18;
        }
    }
}

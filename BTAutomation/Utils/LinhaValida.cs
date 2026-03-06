namespace BTAutomation.Utils
{
    public class LinhaValida
    {
        public static bool EhLinhaValida(string l)
        {
            if (string.IsNullOrWhiteSpace(l)) return false;
            //string[] ignorar = {
            //    "#INIT", "MODEL", "S/W", "INSTRUMENT", "DATE", "TIME", "TESTCODE", "JIG",
            //    "PROGRAM", "MODELFILE", "IMEINO", "INIFILE", "RDM_LOT", "Write", "#END",
            //    "ERROR-CODE", "FAILITEM", "RESULT", "TEST-TIME", "Switch", "ExitMetaMode",
            //    "Check", "Golden", "=", "/*", "P/N", "[", "LoadRfConfig", "#TEST", "Test Conditions"
            //};

            //return !ignorar.Any(prefixo => l.StartsWith(prefixo));

            string[] result = {
                "RESULT"
            };
            return result.Any(prefixo => l.StartsWith(prefixo));
        }
    }
}

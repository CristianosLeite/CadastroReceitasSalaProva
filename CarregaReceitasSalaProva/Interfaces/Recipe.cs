namespace CarregaReceitasSalaProva.Interfaces
{
    internal class Recipe(string[] tag, string[] tValue5, string[] tValue12)
    {
#pragma warning disable IDE1006

        public string[] Tag { get; set; } = tag;
        public string[] T_Value_5 { get; set; } = tValue5;
        public string[] T_Value_12 { get; set; } = tValue12;

#pragma warning restore IDE1006
    }
}

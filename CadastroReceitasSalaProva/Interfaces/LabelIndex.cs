namespace CadastroReceitasSalaProva.Interfaces
{
    public class LabelIndex
    {
        public string Recipe { get; set; }
        public string MinEmpty5 { get; set; }
        public string MaxEmpty5 { get; set; }
        public string Pw5 { get; set; }
        public string MinEmpty12 { get; set; }
        public string MaxEmpty12 { get; set; }
        public string Pw12 { get; set; }

        public LabelIndex()
        {
            Recipe = "";
            MinEmpty5 = "";
            MaxEmpty5 = "";
            Pw5 = "";
            MinEmpty12 = "";
            MaxEmpty12 = "";
            Pw12 = "";
        }
    }
}

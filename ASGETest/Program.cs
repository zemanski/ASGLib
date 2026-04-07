namespace ASGTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ASGLib.Checkers.CheckersGame game = new ASGLib.Checkers.CheckersGame("checkers.txt");
            game.PrintDebug();
            string json = game.Serialize();
            game = ASGLib.Checkers.CheckersGame.Deserialize(json);
            game.PrintDebug();
        }
    }
}

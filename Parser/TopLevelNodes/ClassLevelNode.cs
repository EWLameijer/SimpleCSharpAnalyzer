using Tokenizing;

namespace Parser.TopLevelNodes;

internal class ClassLevelNode
{
    //private static readonly SqlConnection _con = new("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=phoneshop;Integrated Security=T" +
    //                                                 "rue;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent" +
    //                                                 "=ReadWrite;MultiSubnetFailover=False");
    public static ClassLevelNode Get(ParsePosition position)
    {
        List<Token> modifiers = new();
        while (position.CurrentTokenType().IsModifier())
        {
            modifiers.Add(position.CurrentToken());
            position.Proceed();
        }
        TypeNode typeNode = TypeNode.Get(position)
    }
}
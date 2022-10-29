using Tokenizing;

namespace ParserTests;

public class UnitTest1
{
    private const string TestFile = @"using Phoneshop.Domain.Interfaces;
using System.Data.SqlClient;

namespace Phoneshop.Business;
public class BrandRepository : IBrandRepository
{
    private static readonly SqlConnection _con = new(""Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=phoneshop;Integrated Security=T"" +
                                                     ""rue;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent"" +
                                                     ""=ReadWrite;MultiSubnetFailover=False"");
    public void Add(string brand)
    {
        SqlCommand cmd = new($""INSERT INTO brands VALUES (@brand)"", _con);
        cmd.Parameters.AddWithValue(""@brand"", brand);
        _con.Open();
        cmd.ExecuteNonQuery();
        _con.Close();
    }

    public void Delete(string brand)
    {
        SqlCommand cmd = new($""DELETE FROM brands WHERE BrandName = @brand"", _con);
        cmd.Parameters.AddWithValue(""@brand"", brand);
        _con.Open();
        cmd.ExecuteNonQuery();
        _con.Close();
    }

    public bool Exists(string brand)
    {
        SqlCommand cmd = new($""SELECT * FROM brands WHERE BrandName = @brandname"", _con);
        cmd.Parameters.AddWithValue(""@brandname"", brand);
        return ExistsQuery(cmd);
    }

    public bool Exists(int id)
    {
        SqlCommand cmd = new($""SELECT * FROM brands WHERE BrandId = @brandid"", _con);
        cmd.Parameters.AddWithValue(""@brandid"", id);
        return ExistsQuery(cmd);
    }

    private static bool ExistsQuery(SqlCommand cmd)
    {
        _con.Open();
        SqlDataReader dr = cmd.ExecuteReader();
        bool output = dr.Read();
        dr.Close();
        _con.Close();
        return output;
    }

    public string? Get(int id)
    {
        SqlCommand cmd = new($""SELECT BrandName FROM brands WHERE BrandId = {id}"", _con);
        _con.Open();
        SqlDataReader dr = cmd.ExecuteReader();
        if (!dr.Read())
        {
            dr.Close();
            _con.Close();
            return null;
        }
        string output = dr.GetString(0);
        dr.Close();
        _con.Close();
        return output;
    }

    public int? GetId(string brand)
    {
        SqlCommand cmd = new($""SELECT BrandId FROM brands WHERE BrandName = @brandname"", _con);
        cmd.Parameters.AddWithValue(""@brandname"", brand);
        _con.Open();
        SqlDataReader dr = cmd.ExecuteReader();
        if (!dr.Read())
        {
            dr.Close();
            _con.Close();
            return null;
        }
        int output = dr.GetInt32(0);
        dr.Close();
        _con.Close();
        return output;
    }
}";

    [Fact]
    public void Should_return_two_usings_nodes()
    {
        Tokenizer tokenizer = new(TestFile.Split('\n'));

        Console.WriteLine($"\n***{fileData.ContextedFilename}***");

        while (tokenizer.HasNextToken())
        {
            tokenizer.Get();
        }
        IReadOnlyList<Token> tokens = tokenizer.Results();
    }
}
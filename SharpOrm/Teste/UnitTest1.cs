using Microsoft.VisualStudio.TestTools.UnitTesting;
using MySql.Data.MySqlClient;
using SharpOrm;
using SharpOrm.Builder;
using Teste.Utils;

namespace Teste
{
    [TestClass]
    public class UnitTest1 : MysqlTableTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            //define string de conexao e cria a conexao

            var query = new Query(connection, TABLE + " a")
            {
            };
            query.Join(TABLE + " b", "a.id", "=", "b.name");

            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                System.Diagnostics.Debug.WriteLine(reader[1]);
            }

            //using var cmd = mConn.CreateCommand();
            //cmd.CommandText = "SELECT * FROM cadastro.teste WHERE id = @p1 AND Id = @p2;";
            //cmd.Parameters.Add(new MySqlParameter("@p1", 1));
            //cmd.Parameters.Add(new MySqlParameter("@p2", 2));
            //cmd.ExecuteReader();
        }
    }
}

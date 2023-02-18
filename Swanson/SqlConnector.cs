using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.Windows;
using System.IO;

namespace Swanson
{
    class SqlConnector
    {
        private static OleDbConnection connection = new OleDbConnection();
        private OleDbCommand command;

        public static OleDbConnection Connection { get => connection; set => connection = value; }

        public SqlConnector(string path)
        {
            Connection.ConnectionString = $"Provider=Microsoft.ACE.OLEDB.12.0; Data Source={path}Graph.accdb; Persist Security Info=False;";
            command = Connection.CreateCommand();
        }

        public void Write(List<SampleVertex> graph, string graphName, string DFSTime, string BFSTime)
        {
            try
            {
                Connection.Open();
                string sql = $"INSERT INTO Graph (GraphName, DFSTime, BFSTime) " +
                    $"VALUES('{graphName}', '{DFSTime}', '{BFSTime}')";
                command.CommandText = sql;
                command.ExecuteNonQuery();

                foreach (SampleVertex v in graph)  
                {
                    sql = $"INSERT INTO Vertices (Vertex, Edges, FK_GraphName) " +
                    $"VALUES('{v.Text}', '{v.Adjecent}', '{graphName}')";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                Connection.Close();
            }
            catch (OleDbException ex)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error Write()");
            }
        }

        public DataTable ReadGraphs()
        {
            DataTable dt = new DataTable();
            bool i = Environment.Is64BitProcess; // If this is true it wont work, in configuration menager process must be set to x86
            try
            {
                Connection.Open();
                string sql = "SELECT GraphName FROM Graph";
                command.CommandText = sql;
                OleDbDataAdapter da = new OleDbDataAdapter(command);
                da.Fill(dt);
                Connection.Close();
            }
            catch (Exception)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error ReadGraphs()");
            }

            return dt;
        }

        public DataTable ReadGraphTimes(string graphName)
        {
            DataTable dt = new DataTable();

            try
            {
                Connection.Open();
                string sql = "SELECT DFSTime, BFSTime FROM Graph " +
                    $"WHERE GraphName = '{graphName}'";
                command.CommandText = sql;
                OleDbDataAdapter da = new OleDbDataAdapter(command);
                da.Fill(dt);
                Connection.Close();
            }
            catch (Exception)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error ReadGraphTimes()");
            }

            return dt;
        }

        public DataTable ReadVertices(string graphName)
        {
            DataTable dt = new DataTable();

            try
            {
                Connection.Open();
                string sql = "SELECT Vertex, Edges FROM Vertices " +
                    $"WHERE FK_GraphName = '{graphName}'";
                command.CommandText = sql;
                OleDbDataAdapter da = new OleDbDataAdapter(command);
                da.Fill(dt);
                Connection.Close();
            }
            catch (Exception)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error ReadVertices()");
            }

            return dt;
        }

        public void Delete(string graphName)
        {
            try
            {
                Connection.Open();
                
                string sql = $"DELETE * " +
                    $"FROM Graph WHERE GraphName = '{graphName}'";
                command.CommandText = sql;
                command.ExecuteNonQuery();

                sql = $"DELETE * " +
                    $"FROM Vertices WHERE FK_GraphName = '{graphName}'";
                command.CommandText = sql;
                command.ExecuteNonQuery();

                Connection.Close();
            }
            catch (OleDbException ex)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error Delete()");
            }
        }

        public void Edit(List<SampleVertex> graph, string graphName, string DFSTime, string BFSTime)
        {
            try
            {
                Connection.Open();
                string sql = $" UPDATE Graph " +
                    $"SET DFSTime = '{DFSTime}', BFSTime = '{BFSTime}' " +
                    $"WHERE(GraphName = '{graphName}')";
                command.CommandText = sql;
                command.ExecuteNonQuery();

                foreach (SampleVertex v in graph)
                {
                    sql = $" UPDATE Vertices " +
                    $"SET Vertex = '{v.Text}', Edges = '{v.Adjecent}' " +
                    $"WHERE(FK_GraphName = '{graphName}')";
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }

                Connection.Close();
            }
            catch (OleDbException ex)
            {
                if (Connection != null)
                    Connection.Close();
                MessageBox.Show("Error Edit()");
            }
        }
    }
}

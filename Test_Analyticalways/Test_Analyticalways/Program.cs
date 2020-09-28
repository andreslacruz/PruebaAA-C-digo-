using LumenWorks.Framework.IO.Csv;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;

namespace Test_Analyticalways
{
    class Program
    {
        private static string ServerName = "";
        private static string Database = "Test_AnalyticalwaysDB";
        private static string UserId = "";
        private static string Password = "";

        private static string ConnectionString = "Server=" + ServerName + "; Database=" + Database + "; User Id = " + UserId + "; Password=" + Password + ";";

        private static string PathTarget = "C:\\Stock.CSV";

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("Inicio de la Descarga Stock.CSV");
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromDays(1);
            var response = await client.GetAsync(@"https://storage10082020.blob.core.windows.net/y9ne9ilzmfld/Stock.CSV");
            Console.WriteLine("Fin  de la Descarga Stock.CSV");

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var fileInfo = new FileInfo("Stock.CSV");
                if (File.Exists(PathTarget))
                    File.Delete(PathTarget);

                File.Copy(fileInfo.FullName, PathTarget);
                File.Delete(fileInfo.FullName);
                Console.WriteLine("Inicio de la Migracion");
                InsertDataByTSQLBulk();
                Console.WriteLine("Inicio de la Migracion");
            }

            #region Codigo Comentado solo para fines de demostrar que otras formas menos eficientes se puede ejecutar la migracion masiva
            //var csvDataTable = new DataTable();
            //using (var stream = await response.Content.ReadAsStreamAsync())
            //{
            //    var fileInfo = new FileInfo("Stock.CSV");
            //    using (var fileStream = new CsvReader(new StreamReader(fileInfo.OpenRead()), true, ';'))
            //    {
            //        csvDataTable.Load(fileStream);
            //    }
            //}

            //if (csvDataTable != null)
            //    InsertDataBySQLBulk(csvDataTable);
            #endregion

        }

        //Promedio de tiempo de ejecucion 3,303 Minutos
        private static void InsertDaTaByQuery(DataTable dt)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            string QueryCreateType = @"CREATE TYPE StoreTemp_Type AS TABLE(
                                                [PointOfSale] [nvarchar](100) NULL,
                                                [Product] [nvarchar](100) NULL,
                                                [Date] [nvarchar](100) NULL,
                                                [Stock] [int] NULL
                                                )";

            string QueryCreateSP = @" CREATE PROCEDURE [InsertTableStoreTemp_SP]
                                                    @DataTableStock StoreTemp_Type readonly
                                                AS
                                                BEGIN

	                                                IF EXISTS (SELECT * FROM Store)
	                                                BEGIN
		                                                DELETE FROM STORE
	                                                END

                                                    INSERT INTO Store SELECT * FROM @DataTableStock 
                                                END";


            string QueryDeleteSPAndType = @"DROP PROCEDURE IF EXISTS InsertTableStoreTemp_SP
                                            DROP TYPE IF EXISTS StoreTemp_Type";

            try
            {
                SqlConnection conn = new SqlConnection(ConnectionString);
                SqlCommand cmdCreateType = new SqlCommand(QueryCreateType, conn);
                cmdCreateType.CommandType = CommandType.Text;


                SqlCommand cmdCreateSP = new SqlCommand(QueryCreateSP, conn);
                cmdCreateSP.CommandType = CommandType.Text;

                SqlCommand cmdInsertTable = new SqlCommand("InsertTableStoreTemp_SP", conn);
                cmdInsertTable.CommandType = CommandType.StoredProcedure;
                SqlParameter sqlParameter = cmdInsertTable.Parameters.AddWithValue("@DataTableStock", dt);
                sqlParameter.SqlDbType = SqlDbType.Structured;
                sqlParameter.TypeName = "StoreTemp_Type";


                SqlCommand cmdDelete = new SqlCommand(QueryDeleteSPAndType, conn);
                cmdDelete.CommandType = CommandType.Text;

                cmdInsertTable.CommandTimeout = 90000;

                conn.Open();
                cmdDelete.ExecuteNonQuery();
                cmdCreateType.ExecuteNonQuery();
                cmdCreateSP.ExecuteNonQuery();
                cmdInsertTable.ExecuteNonQuery();
                cmdDelete.ExecuteNonQuery();
                conn.Close();
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs.ToString());
        }

        //Promedio de tiempo de ejecucion 3,245 Minutos
        private static void InsertDataBySQLBulk(DataTable dt)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            string QueryDeleteTable = @"IF EXISTS (SELECT * FROM Store)
	                                 BEGIN
		                                DELETE FROM STORE
	                                 END";

            try
            {
                using (SqlConnection conn = new SqlConnection(ConnectionString))
                {
                    SqlCommand cmdDelete = new SqlCommand(QueryDeleteTable, conn);


                    conn.Open();

                    cmdDelete.ExecuteNonQuery();

                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn))
                    {
                        bulkCopy.BulkCopyTimeout = 90000;

                        bulkCopy.DestinationTableName = "Store";

                        foreach (var column in dt.Columns)
                            bulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());

                        bulkCopy.WriteToServer(dt);
                    }
                    conn.Close();
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }

            watch.Stop();
            var elapsedMs = watch.ElapsedMilliseconds;
            Console.WriteLine(elapsedMs.ToString());
        }

        //Promedio de tiempo de ejecucion 2,483
        private static void InsertDataByTSQLBulk()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            string QueryDeleteTable = @"IF EXISTS (SELECT * FROM Store)
	                                     BEGIN
		                                    DROP TABLE store
                                            CREATE TABLE [dbo].[Store](
	                                            [PointOfSale] [nvarchar](100) NULL,
	                                            [Product] [nvarchar](100) NULL,
	                                            [Date] [Datetime] NULL,
	                                            [Stock] [int] NULL
                                            ) ON [PRIMARY]
	                                     END";

            string QueryBulk = @"BULK
                                 INSERT [Store]
                                 FROM '" + PathTarget + @"'
                                 WITH
                                 (
                                    FIELDTERMINATOR = ';'
                                 )";
            try
            {
                SqlConnection conn = new SqlConnection(ConnectionString);

                SqlCommand cmdDeleteTable = new SqlCommand(QueryDeleteTable, conn);
                cmdDeleteTable.CommandType = CommandType.Text;

                SqlCommand cmdBulk = new SqlCommand(QueryBulk, conn);
                cmdBulk.CommandType = CommandType.Text;

                cmdDeleteTable.CommandTimeout = 90000;
                cmdBulk.CommandTimeout = 90000;

                conn.Open();
                cmdDeleteTable.ExecuteNonQuery();
                cmdBulk.ExecuteNonQuery();
                conn.Close();

            }
            catch (SqlException ex)
            {
                if (ex.Message != "Bulk load data conversion error (type mismatch or invalid character for the specified codepage) for row 1, column 3 (Date).")
                    Console.WriteLine(ex.Message);
            }

            watch.Stop();
            var elapsedMs = watch.Elapsed.TotalMinutes;
            Console.WriteLine("Tiempo de ejecucion de la migracion: " + elapsedMs.ToString());
        }
    }
}

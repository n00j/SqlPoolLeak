using System;
using System.Threading;
using System.Data.SqlClient;
using System.Collections.Generic;

namespace PoolConnectionTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Random rnd = new Random(DateTime.Now.Millisecond);
            var monitors = new List<MonitorChanges>();
            var connectionString = "Data Source=INTGCD01AG01L.something.com,28589;Initial Catalog=sltag;User ID=<USERID>;Password=<PASSWORD>;multisubnetfailover=yes;";
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.MaxPoolSize = 10;
            builder.MinPoolSize = 1;

            for (var i = 0; i < 10; ++i)
            {
                var ch = new MonitorChanges();
                ch.Start(builder);
                monitors.Add(ch);
            }

            while(true)
            {
                Console.WriteLine("Sleeing...");
                Thread.Sleep(1000);
            }

        }
	private class MonitorChanges {
            private static Random rnd = new Random(DateTime.Now.Millisecond);
            private Timer _t;
            private object _refreshLock = new object();

            private void executeSql(object state)
            {
                if (!Monitor.TryEnter(_refreshLock))
                	return; // refresh data already running

                try
                {
                    var builder = (SqlConnectionStringBuilder)state;
                    Console.WriteLine(builder.ConnectionString);
                    using (var conn = new SqlConnection(builder.ConnectionString))
                    {
                        var sqlQuery = "SELECT 1";
                        using (var cmd = new SqlCommand(sqlQuery, conn))
                        {
                            cmd.CommandTimeout = 20;
                            conn.Open();

                            using (var sdr = cmd.ExecuteReader())
                            {
                                sdr.Read();
                                var intRet = sdr.GetInt32(0);
                                Console.WriteLine(intRet);
                            }

                        }
                    }
                }
		catch (Exception e) 
                {
                    Console.WriteLine($"Exception Caught: {e.Message}\n{e.StackTrace}");
                }
                finally
                {
                    Monitor.Exit(_refreshLock);
                }
            }

            public void Start(object builder) {
                var t = new Timer(executeSql, builder, rnd.Next(1, 1000), 1000 + rnd.Next(1, 1000));
                _t = t;
            }        
        }
    }
}

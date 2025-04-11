using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace Read4All.Database
{
    class TestConnection
    {
        public async Task TestConnectionAsync()
        {
            try
            {
                using (var connection = new MySqlConnection("Server=10.0.2.2;Port=6033;Database=db_livre;User=root;Password=root;"))
                {
                    await connection.OpenAsync();
                    Debug.WriteLine("Works " + connection.ConnectionString);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Doesnt work lol" + ex);
            }
        }
    }
}

using Npgsql;
using PrimeAppBooks.Configurations;
using PrimeAppBooks.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrimeAppBooks.Services
{
    public class SplashscreenInitialisations
    {
        private BoxServices _messageBoxService = new();

        public bool TestConnectionToDatabase()
        {
            try
            {
                using (var testConn = new NpgsqlConnection(AppConfig.DefaultConnection))
                {
                    testConn.Open();
                    //_messageBoxService.ShowMessage("Succcessfully connected", "Information!", "InfoOutline");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _messageBoxService.ShowMessage(ex.ToString(), "Error Connecting to database.", "ErrorOutline");
            }
            return false;
        }
    }
}
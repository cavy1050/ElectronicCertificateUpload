using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Ioc;
using System.Data.SQLite;
using System.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace ElectronicCertificateUpload.Services
{
    internal class NativeDbController : AsyncDbControllerBase
    {
        internal override IDbConnection Connection
        {
            get => new SQLiteConnection(Provider.NativeDBConnectString);
        }

        internal NativeDbController(IContainerProvider containerProviderArg) : base(containerProviderArg) { }
    }

    internal class MZDbController : AsyncDbControllerBase
    {
        internal override IDbConnection Connection
        {
            get => new SqlConnection(Provider.MZDBConnectString);
        }

        internal MZDbController(IContainerProvider containerProviderArg) : base(containerProviderArg) { }
    }

    internal class ZYDbController : AsyncDbControllerBase
    {
        internal override IDbConnection Connection
        {
            get => new SqlConnection(Provider.ZYDBConnectString);
        }

        internal ZYDbController(IContainerProvider containerProviderArg) : base(containerProviderArg) { }
    }

    internal class DZPJDbController : AsyncDbControllerBase
    {
        internal override IDbConnection Connection
        {
            get => new MySqlConnection(Provider.DZPJDBConnectString);
        }

        internal DZPJDbController(IContainerProvider containerProviderArg) : base(containerProviderArg) { }
    }
}

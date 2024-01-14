using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElectronicCertificateUpload.Services
{
    internal interface IAsyncDbController
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sqlSentenceArg);

        Task<int> ExecuteAsync(string sqlSentenceArg);

        Task<dynamic> QueryFirstOrDefaultAsync(string sqlSentenceArg);

        Task<IEnumerable<dynamic>> QueryAsync(string sqlSentenceArg);
    }
}

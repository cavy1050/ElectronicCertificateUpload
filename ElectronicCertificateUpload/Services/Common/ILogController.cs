using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    internal interface ILogController
    {
        void WriteDebug(LogMessageKind logMessageKindArg);

        void WriteError(LogMessageKind logMessageKindArg);
    }
}

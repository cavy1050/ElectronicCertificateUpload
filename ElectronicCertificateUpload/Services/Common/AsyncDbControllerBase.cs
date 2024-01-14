using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Prism.Ioc;
using Dapper;
using ElectronicCertificateUpload.Core;

namespace ElectronicCertificateUpload.Services
{
    internal abstract class AsyncDbControllerBase : IAsyncDbController
    {
        ILogController logController;
        CommandDefinition command;

        internal abstract IDbConnection Connection { get; }

        internal AsyncDbControllerBase(IContainerProvider containerProviderArg)
        {
            logController = containerProviderArg.Resolve<ILogController>();
        }

        public virtual async Task<IEnumerable<T>> QueryAsync<T>(string sqlSentenceArg)
        {
            IEnumerable<T> values = default(IEnumerable<T>);

            command = new CommandDefinition(sqlSentenceArg);

            logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryComment = sqlSentenceArg, QueryStatus = "Open" });

            try
            {
                Connection.Open();
                values = await SqlMapper.QueryAsync<T>(Connection, command);
            }
            catch (Exception ex)
            {
                logController.WriteError(new LogMessageKind { Level = "Error", ClassName = ex.Source, FunctionName = ex.TargetSite.Name, ErrorMessage = ex.Message });
            }
            finally
            {
                Connection.Close();
                logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryStatus = "Close" });
            }

            return values;
        }

        public virtual async Task<int> ExecuteAsync(string sqlSentenceArg)
        {
            int value = default(int);

            command = new CommandDefinition(sqlSentenceArg);

            logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryComment = sqlSentenceArg, QueryStatus = "Open" });

            try
            {
                Connection.Open();
                value = await SqlMapper.ExecuteAsync(Connection, command);
            }
            catch (Exception ex)
            {
                logController.WriteError(new LogMessageKind { Level = "Error", ClassName = ex.Source, FunctionName = ex.TargetSite.Name, ErrorMessage = ex.Message });
            }
            finally
            {
                Connection.Close();
                logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryStatus = "Close" });
            }

            return value;
        }

        public virtual async Task<dynamic> QueryFirstOrDefaultAsync(string sqlSentenceArg)
        {
            dynamic value = default(dynamic);

            command = new CommandDefinition(sqlSentenceArg);

            logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryComment = sqlSentenceArg, QueryStatus = "Open" });

            try
            {
                Connection.Open();
                value = await SqlMapper.QueryFirstOrDefaultAsync(Connection, command);
            }
            catch (Exception ex)
            {
                logController.WriteError(new LogMessageKind { Level = "Error", ClassName = ex.Source, FunctionName = ex.TargetSite.Name, ErrorMessage = ex.Message });
            }
            finally
            {
                Connection.Close();
                logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryStatus = "Close" });
            }

            return value;
        }

        public virtual async Task<IEnumerable<dynamic>> QueryAsync(string sqlSentenceArg)
        {
            dynamic values = default(IEnumerable<dynamic>);

            command = new CommandDefinition(sqlSentenceArg);

            logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryComment = sqlSentenceArg, QueryStatus = "Open" });

            try
            {
                Connection.Open();
                values = await SqlMapper.QueryAsync<dynamic>(Connection, command);
            }
            catch (Exception ex)
            {
                logController.WriteError(new LogMessageKind { Level = "Error", ClassName = ex.Source, FunctionName = ex.TargetSite.Name, ErrorMessage = ex.Message });
            }
            finally
            {
                Connection.Close();
                logController.WriteDebug(new LogMessageKind { Level = "Debug", QueryStatus = "Close" });
            }

            return values;
        }
    }
}

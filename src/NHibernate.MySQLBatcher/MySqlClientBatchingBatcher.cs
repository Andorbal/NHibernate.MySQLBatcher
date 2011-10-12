/*
NHibernate.MySQLBatcher -- Enables NHibernate to batch commands when using MySQL.
Copyright (C) 2011  Oleg Sakharov

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */﻿

using System.Data;
using System.Data.Common;
using System.Text;
using MySql.Data.MySqlClient;
﻿using NHibernate.AdoNet;
﻿using NHibernate.AdoNet.Util;
using NHibernate.Exceptions;

namespace NHibernate.MySQLBatcher {
  public class MySqlClientBatchingBatcher : AbstractBatcher {
    private int batchSize;
    private int totalExpectedRowsAffected;
    private MySqlClientSqlCommandSet currentBatch;
    private StringBuilder currentBatchCommandsLog;

    public MySqlClientBatchingBatcher(ConnectionManager connectionManager, IInterceptor interceptor)
      : base(connectionManager, interceptor) {
      batchSize = Factory.Settings.AdoBatchSize;
      currentBatch = CreateConfiguredBatch();

      //we always create this, because we need to deal with a scenario in which
      //the user change the logging configuration at runtime. Trying to put this
      //behind an if(log.IsDebugEnabled) will cause a null reference exception 
      //at that point.
      currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
    }

    public override int BatchSize {
      get { return batchSize; }
      set { batchSize = value; }
    }

    protected override int CountOfStatementsInCurrentBatch {
      get { return currentBatch.CountOfCommands; }
    }

    public override void AddToBatch(IExpectation expectation) {
      totalExpectedRowsAffected += expectation.ExpectedRowCount;
      IDbCommand batchUpdate = CurrentCommand;
      Prepare((MySqlCommand)batchUpdate);
      Driver.AdjustCommand(batchUpdate);
      string lineWithParameters = null;
      var sqlStatementLogger = Factory.Settings.SqlStatementLogger;
      if (sqlStatementLogger.IsDebugEnabled || log.IsDebugEnabled) {
        lineWithParameters = sqlStatementLogger.GetCommandLineWithParameters(batchUpdate);
        var formatStyle = sqlStatementLogger.DetermineActualStyle(FormatStyle.Basic);
        lineWithParameters = formatStyle.Formatter.Format(lineWithParameters);
        currentBatchCommandsLog.Append("command ")
          .Append(currentBatch.CountOfCommands)
          .Append(":")
          .AppendLine(lineWithParameters);
      }
      if (log.IsDebugEnabled) {
        log.Debug("Adding to batch:" + lineWithParameters);
      }
      currentBatch.Append((MySqlCommand)batchUpdate);

      if (currentBatch.CountOfCommands >= batchSize) {
        DoExecuteBatch(batchUpdate);
      }
    }

    protected override void DoExecuteBatch(IDbCommand ps) {
      log.DebugFormat("Executing batch");
      CheckReaders();
      if (Factory.Settings.SqlStatementLogger.IsDebugEnabled) {
        Factory.Settings.SqlStatementLogger.LogBatchCommand(currentBatchCommandsLog.ToString());
        currentBatchCommandsLog = new StringBuilder().AppendLine("Batch commands:");
      }

      int rowsAffected;
      try {
        rowsAffected = currentBatch.ExecuteNonQuery();
      }
      catch (DbException e) {
        throw ADOExceptionHelper.Convert(Factory.SQLExceptionConverter, e, "could not execute batch command.");
      }

      Expectations.VerifyOutcomeBatched(totalExpectedRowsAffected, rowsAffected);

      currentBatch.Dispose();
      totalExpectedRowsAffected = 0;
      currentBatch = CreateConfiguredBatch();
    }

    private MySqlClientSqlCommandSet CreateConfiguredBatch() {
      return new MySqlClientSqlCommandSet(batchSize);
    }
  }
}
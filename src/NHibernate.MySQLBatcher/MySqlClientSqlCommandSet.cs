﻿/*
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

using System;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace NHibernate.MySQLBatcher {
  public class MySqlClientSqlCommandSet : IDisposable {
    private static System.Type sqlCmdSetType;
    private object instance;
    private InitialiseCommand doInitialise;
    private PropSetter<int> batchSizeSetter;
    private AppendCommand doAppend;
    private ExecuteNonQueryCommand doExecuteNonQuery;
    private DisposeCommand doDispose;
    private int countOfCommands = 0;

    static MySqlClientSqlCommandSet() {
      Assembly sysData = Assembly.Load("MySql.Data");
      sqlCmdSetType = sysData.GetType("MySql.Data.MySqlClient.MySqlDataAdapter");
      Debug.Assert(sqlCmdSetType != null, "Could not find MySqlDataAdapter!");
    }

    public MySqlClientSqlCommandSet(int batchSize) {
      instance = Activator.CreateInstance(sqlCmdSetType, true);
      doInitialise = (InitialiseCommand)Delegate.CreateDelegate(typeof(InitialiseCommand), instance, "InitializeBatching");
      batchSizeSetter = (PropSetter<int>)Delegate.CreateDelegate(typeof(PropSetter<int>), instance, "set_UpdateBatchSize");
      doAppend = (AppendCommand)Delegate.CreateDelegate(typeof(AppendCommand), instance, "AddToBatch");
      doExecuteNonQuery = (ExecuteNonQueryCommand)Delegate.CreateDelegate(typeof(ExecuteNonQueryCommand), instance, "ExecuteBatch");
      doDispose = (DisposeCommand)Delegate.CreateDelegate(typeof(DisposeCommand), instance, "Dispose");

      Initialise(batchSize);
    }

    private void Initialise(int batchSize) {
      doInitialise();
      batchSizeSetter(batchSize);
    }

    public void Append(MySqlCommand command) {
      doAppend(command);
      countOfCommands++;
    }

    public void Dispose() {
      doDispose();
    }

    public int ExecuteNonQuery() {
      try {
        if (CountOfCommands == 0) {
          return 0;
        }

        return doExecuteNonQuery();
      }
      catch (Exception exception) {
        throw new HibernateException("An exception occured when executing batch queries", exception);
      }
    }

    public int CountOfCommands {
      get {
        return countOfCommands;
      }
    }

    #region Delegate Definations

    private delegate void PropSetter<T>(T item);

    private delegate void InitialiseCommand();

    private delegate int AppendCommand(IDbCommand command);

    private delegate int ExecuteNonQueryCommand();

    private delegate void DisposeCommand();

    #endregion
  }
}
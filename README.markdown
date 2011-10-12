# NHibernate.MySQLBatcher

NHibernate.MySQLBatcher is a simple library that allows NHibernate to batch MySQL commands.  The was initially [https://nhibernate.jira.com/browse/NH-2778](a patch) built by Oleg Sakharov, but could not be included into NHibernate because of the dependency on mysql.data.

## Usage

I plan on building a NuGet package for this library, but until that is finished the easiest way to use the library is to build the assembly, add it to your project, and then add the following line to your NHibernate configuration:

    config.DataBaseIntegration(
        db => db.Batcher<MySqlClientBatchingBatcherFactory>());

Thanks to Diego Mijelshon for [http://stackoverflow.com/questions/6900594/why-doesnt-nhibernate-support-batching-on-mysql](pointing this functionality out).
# NHibernate.MySQLBatcher

NHibernate.MySQLBatcher is a simple library that allows NHibernate to batch MySQL commands.  The was initially [a patch](https://nhibernate.jira.com/browse/NH-2778) built by Oleg Sakharov, but could not be included into NHibernate because of the dependency on mysql.data.

## Usage

The easiest way to use the library is to install it via NuGet:

    Install-Package NHibernate.MySQLBatcher

Then, add the following line to your NHibernate configuration:

    config.DataBaseIntegration(
        db => db.Batcher<MySqlClientBatchingBatcherFactory>());

Thanks to Diego Mijelshon for [pointing this functionality out](http://stackoverflow.com/questions/6900594/why-doesnt-nhibernate-support-batching-on-mysql).

## License

This library is licensed under the [LGPL v2.1](http://www.gnu.org/licenses/lgpl-2.1-standalone.html) because that's what NHibernate is licensed under and I want to stay consistent with that.
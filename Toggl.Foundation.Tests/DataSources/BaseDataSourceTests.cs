using System;
using NSubstitute;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Tests.DataSources
{
    public abstract class BaseDataSourceTests<TDataSource>
    {
        protected IIdProvider IdProvider { get; } = Substitute.For<IIdProvider>();
        protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();
        protected ITogglDatabase DataBase { get; } = Substitute.For<ITogglDatabase>();

        protected TDataSource DataSource { get; private set; }

        protected BaseDataSourceTests()
        {
            Setup();
        }

        private void Setup()
        {
            DataSource = CreateDataSource();
        }

        protected abstract TDataSource CreateDataSource();
    }
}

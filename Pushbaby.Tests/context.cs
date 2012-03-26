using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;

namespace Pushbaby.Tests
{
    public abstract class context
    {
        protected Exception Exception;

        [TestFixtureSetUp]
        protected void TestFixtureSetUp()
        {
            before_given();
            given();

            if (this.ExpectException())
            {
                try { this.when(); }
                catch (Exception ex) { this.Exception = ex; }

                this.Exception.Should().NotBeNull("The 'when' method was marked with [throws] but no exception was caught.");
            }
            else
            {
                this.when();
            }
        }

        [TestFixtureTearDown]
        protected void TestFixtureTearDown()
        {
            cleanup();
        }

        protected virtual void before_given() { }
        protected virtual void given() { }
        protected virtual void when() { }
        protected virtual void cleanup() { }

        bool ExpectException()
        {
            return (from m in this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    where m.Name == "when"
                    from a in m.GetCustomAttributes(false)
                    where a is throwsAttribute
                    select m).Any();
        }
    }

    public class thenAttribute : TestAttribute { }
    public class throwsAttribute : Attribute { }
}

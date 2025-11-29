using SharpOrm;
using System;
using System.Collections.Generic;
using Xunit;

namespace QueryTest
{
    public class DisposeUtilsTests
    {
        [Fact]
        public void SafeDispose_WithNullDisposable_ShouldNotThrow()
        {
            // Act & Assert
            DisposeUtils.SafeDispose(null);
            DisposeUtils.SafeDispose(null, "Test context");
        }

        [Fact]
        public void SafeDispose_WithValidDisposable_ShouldDisposeSuccessfully()
        {
            // Arrange
            var disposable = new TestDisposable();
            Assert.False(disposable.IsDisposed);

            // Act
            DisposeUtils.SafeDispose(disposable);

            // Assert
            Assert.True(disposable.IsDisposed);
        }

        [Fact]
        public void SafeDispose_WithDisposableThatThrows_ShouldNotPropagateException()
        {
            // Arrange
            var disposable = new ThrowingDisposable();
            string capturedContext = null;
            Exception capturedException = null;
            DisposeUtils.ExceptionLogger = (context, ex) =>
            {
                capturedContext = context;
                capturedException = ex;
            };

            // Act
            DisposeUtils.SafeDispose(disposable, "Test context");

            // Assert
            Assert.NotNull(capturedException);
            Assert.NotNull(capturedContext);
            Assert.Equal("Test context", capturedContext);
            Assert.IsType<InvalidOperationException>(capturedException);
        }

        [Fact]
        public void SafeDispose_WithDisposableThatThrows_WithoutLogger_ShouldNotThrow()
        {
            // Arrange
            var disposable = new ThrowingDisposable();
            DisposeUtils.ExceptionLogger = null;

            // Act & Assert - Should not throw
            DisposeUtils.SafeDispose(disposable);
        }

        [Fact]
        public void SafeExecute_WithNullAction_ShouldNotThrow()
        {
            // Act & Assert
            DisposeUtils.SafeExecute(null);
            DisposeUtils.SafeExecute(null, "Test context");
        }

        [Fact]
        public void SafeExecute_WithValidAction_ShouldExecuteSuccessfully()
        {
            // Arrange
            var executed = false;
            Action action = () => executed = true;

            // Act
            DisposeUtils.SafeExecute(action);

            // Assert
            Assert.True(executed);
        }

        [Fact]
        public void SafeExecute_WithActionThatThrows_ShouldNotPropagateException()
        {
            // Arrange
            Exception capturedException = null;
            DisposeUtils.ExceptionLogger = (context, ex) => capturedException = ex;
            Action action = () => throw new InvalidOperationException("Test exception");

            // Act
            DisposeUtils.SafeExecute(action, "Test context");

            // Assert
            Assert.NotNull(capturedException);
            Assert.IsType<InvalidOperationException>(capturedException);
            Assert.Equal("Test exception", capturedException.Message);
        }

        [Fact]
        public void SafeExecute_WithActionThatThrows_WithoutLogger_ShouldNotThrow()
        {
            // Arrange
            DisposeUtils.ExceptionLogger = null;
            Action action = () => throw new InvalidOperationException("Test exception");

            // Act & Assert - Should not throw
            DisposeUtils.SafeExecute(action);
        }

        [Fact]
        public void ExceptionLogger_WhenSet_ShouldBeCalledOnException()
        {
            // Arrange
            var loggedContexts = new List<string>();
            var loggedExceptions = new List<Exception>();
            DisposeUtils.ExceptionLogger = (context, ex) =>
            {
                loggedContexts.Add(context);
                loggedExceptions.Add(ex);
            };

            var disposable = new ThrowingDisposable();

            // Act
            DisposeUtils.SafeDispose(disposable, "Custom context");

            // Assert
            Assert.Single(loggedContexts);
            Assert.Equal("Custom context", loggedContexts[0]);
            Assert.Single(loggedExceptions);
        }

        private class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private class ThrowingDisposable : IDisposable
        {
            public void Dispose()
            {
                throw new InvalidOperationException("Dispose failed");
            }
        }
    }
}


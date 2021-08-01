//源作者：吕毅（https://mvp.microsoft.com/en-us/PublicProfile/5003225）
//源文件（以MIT协议开源）：https://github.com/walterlv/sharing-demo/blob/master/src/Walterlv.Core/Threading/AwaiterInterfaces.cs

/* MIT License
Copyright(c) 2017 walterlv lanq

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */


using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;

namespace Cemit.Awaiter
{
    /// <summary>
    /// 表示可以等待一个主要运行在 UI 线程的异步操作。
    /// </summary>
    /// <typeparam name="T">异步等待 UI 操作结束后的返回值类型。</typeparam>
    public class AsyncAction<T> : IAwaitable<AsyncAction<T>, T>, IAwaiter<T>
    {
        /// <summary>
        /// 获取一个状态，该状态表示正在异步等待的操作已经完成（成功完成或发生了异常）。
        /// 此状态会被编译器自动调用。
        /// </summary>
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 获取此异步等待操作的返回值。
        /// 与 <see cref="System.Threading.Tasks.Task{TResult}"/> 不同的是，
        /// 如果操作没有完成或发生了异常，此实例会返回 <typeparamref name="T"/> 的默认值，
        /// 而不是阻塞线程直至任务完成。
        /// </summary>
        public T Result { get; private set; }

        /// <summary>临时保存 await 后后续任务的包装，用于报告任务完成后能够继续执行。</summary>
        private Action continuation;

        /// <summary>临时保存异步任务执行过程中发生的异常。它会在异步等待结束后抛出，以报告异步执行过程中发生的错误。</summary>
        private Exception exception;

        /// <summary>
        /// 获取一个可用于 await 关键字异步等待的异步等待对象。
        /// 此方法会被编译器自动调用。
        /// </summary>
        /// <returns>返回自身，用于异步等待返回值。</returns>
        public AsyncAction<T> GetAwaiter()
        {
            return this;
        }

        /// <summary>
        /// 获取此异步等待操作的返回值，此方法会被编译器在 await 结束时自动调用以获取返回值。
        /// 与 <see cref="System.Threading.Tasks.Task{TResult}"/> 不同的是，
        /// 如果操作没有完成，此实例会返回 <typeparamref name="T"/> 的默认值，而不是阻塞线程直至任务完成。
        /// 但是，如果异步操作中发生了异常，调用此方法会抛出这个异常。
        /// </summary>
        /// <returns>
        /// 异步操作的返回值。
        /// </returns>
        public T GetResult()
        {
            if (exception != null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
            return Result;
        }

        /// <summary>
        /// 当使用此类型执行异步任务的方法执行完毕后，编译器会自动调用此方法。
        /// 也就是说，此方法会在调用方所在的线程执行，用于通知调用方所在线程的代码已经执行完毕，请求执行 await 后续任务。
        /// 在此类型中，后续任务是通过 <see cref="Dispatcher.InvokeAsync(Action, DispatcherPriority)"/> 来执行的。
        /// </summary>
        /// <param name="continuation">
        /// 被异步任务状态机包装的后续任务。当执行时，会让状态机继续往下走一步。
        /// </param>
        public void OnCompleted(Action continuation)
        {
            if (IsCompleted)
            {
                // 如果 await 开始时任务已经执行完成，则直接执行 await 后面的代码。
                // 注意，即便 _continuation 有值，也无需关心，因为报告结束的时候就会将其执行。
                continuation?.Invoke();
            }
            else
            {
                // 当使用多个 await 关键字等待此同一个 awaitable 实例时，此 OnCompleted 方法会被多次执行。
                // 当任务真正结束后，需要将这些所有的 await 后面的代码都执行。
                this.continuation += continuation;
            }
        }

        public Exception ThrowException(Exception exception)
        {
            this.exception = exception;
            ReportResult(default);
            return exception;
        }

        /// <summary>
        /// 调用此方法以报告任务结束，并指定返回值和异步任务中的异常。
        /// 当使用 <see cref="Create"/> 静态方法创建此类型的实例后，调用方可以通过方法参数中传出的委托来调用此方法。
        /// </summary>
        /// <param name="result">异步返回值。</param>
        /// <param name="continuationInvokeDelegate">使用一个委托执行后续代码，为空的话则新建一个线程执行。</param>
        public void ReportResult(T result, Action<Action> continuationInvokeDelegate = null)
        {
            Result = result;
            IsCompleted = true;

            // _continuation 可能为 null，说明任务已经执行完毕，但没有任何一处 await 了这个任务。
            if (continuation != null)
            {
                if (continuationInvokeDelegate == null)
                {
                    // 使用一个新的线程调用await后续的代码
                    new Thread(new ThreadStart(continuation)).Start();
                }
                else
                {
                    continuationInvokeDelegate.Invoke(continuation);
                }
            }
        }
    }
}

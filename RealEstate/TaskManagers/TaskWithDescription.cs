using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealEstate.TaskManagers
{
    public class TaskWithDescription : Task
    {
        public string Description { get; set; }

        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the Task.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The action argument is null.
        public TaskWithDescription(Action action) : base(action) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action and
        //     state.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   state:
        //     An object representing data to be used by the action.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The action argument is null.
        public TaskWithDescription(Action<object> action, object state) : base(action, state) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action and
        //     System.Threading.CancellationToken.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the Task.
        //
        //   cancellationToken:
        //     The System.Threading.CancellationToken that the new Task will observe.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The provided System.Threading.CancellationToken has already been disposed.
        //
        //   System.ArgumentNullException:
        //     The action argument is null.
        public TaskWithDescription(Action action, CancellationToken cancellationToken) : base(action, cancellationToken) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action and
        //     creation options.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   creationOptions:
        //     The System.Threading.Tasks.TaskCreationOptions used to customize the Task's
        //     behavior.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The action argument is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     The creationOptions argument specifies an invalid value for System.Threading.Tasks.TaskCreationOptions.
        public TaskWithDescription(Action action, TaskCreationOptions creationOptions) : base(action, creationOptions) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action,
        //     state, and options.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   state:
        //     An object representing data to be used by the action.
        //
        //   cancellationToken:
        //     The System.Threading.Tasks.Task.CancellationToken that that the new task
        //     will observe.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The System.Threading.CancellationTokenSource that created cancellationToken
        //     has already been disposed.
        //
        //   System.ArgumentNullException:
        //     The action argument is null.
        public TaskWithDescription(Action<object> action, object state, CancellationToken cancellationToken) : base(action, state, cancellationToken) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action,
        //     state, and options.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   state:
        //     An object representing data to be used by the action.
        //
        //   creationOptions:
        //     The System.Threading.Tasks.TaskCreationOptions used to customize the Task's
        //     behavior.
        //
        // Exceptions:
        //   System.ArgumentNullException:
        //     The action argument is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     The creationOptions argument specifies an invalid value for System.Threading.Tasks.TaskCreationOptions.
        public TaskWithDescription(Action<object> action, object state, TaskCreationOptions creationOptions) : base(action, state, creationOptions) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action and
        //     creation options.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   cancellationToken:
        //     The System.Threading.Tasks.Task.CancellationToken that the new task will
        //     observe.
        //
        //   creationOptions:
        //     The System.Threading.Tasks.TaskCreationOptions used to customize the Task's
        //     behavior.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The System.Threading.CancellationTokenSource that created cancellationToken
        //     has already been disposed.
        //
        //   System.ArgumentNullException:
        //     The action argument is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     The creationOptions argument specifies an invalid value for System.Threading.Tasks.TaskCreationOptions.
        public TaskWithDescription(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, cancellationToken, creationOptions) { }
        //
        // Summary:
        //     Initializes a new System.Threading.Tasks.Task with the specified action,
        //     state, and options.
        //
        // Parameters:
        //   action:
        //     The delegate that represents the code to execute in the task.
        //
        //   state:
        //     An object representing data to be used by the action.
        //
        //   cancellationToken:
        //     The System.Threading.Tasks.Task.CancellationToken that that the new task
        //     will observe..
        //
        //   creationOptions:
        //     The System.Threading.Tasks.TaskCreationOptions used to customize the Task's
        //     behavior.
        //
        // Exceptions:
        //   System.ObjectDisposedException:
        //     The System.Threading.CancellationTokenSource that created cancellationToken
        //     has already been disposed.
        //
        //   System.ArgumentNullException:
        //     The action argument is null.
        //
        //   System.ArgumentOutOfRangeException:
        //     The creationOptions argument specifies an invalid value for System.Threading.Tasks.TaskCreationOptions.
        public TaskWithDescription(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, state, cancellationToken, creationOptions) { }
    }
}

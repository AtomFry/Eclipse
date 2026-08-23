using System;
using System.Windows.Input;

namespace Eclipse.Helpers
{
    /// <summary>
    /// A command backed by two delegates.
    ///
    /// Replaces Prism's <c>DelegateCommand</c>, which was one of only two things the whole Prism
    /// dependency was carried for - a package the integration notes record as not resolving
    /// cleanly in the host load context, shipped for this and four events.
    ///
    /// Deliberately does not hook <c>CommandManager.RequerySuggested</c>, matching what
    /// <c>DelegateCommand</c> did: bound controls re-ask <see cref="CanExecute"/> only when
    /// <see cref="RaiseCanExecuteChanged"/> says so, which the settings view models already do
    /// through their <c>InvalidateCommands</c> methods.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            this.execute = execute;
            this.canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute();
        }

        public void Execute(object parameter)
        {
            execute();
        }

        /// <summary>
        /// Asks the bound controls to re-evaluate <see cref="CanExecute"/>. Named as Prism named
        /// it, so the call sites read unchanged.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

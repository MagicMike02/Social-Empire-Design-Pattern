namespace Script
{
    public interface ICommand
    {
        void Execute(IWorld world);
        bool CanExecute(IWorld world);
    }
}
using NUnit.Framework;
using Script.Core.Commands;

namespace Tests.EditMode.Commands
{
    public class CommandHistoryTests
    {
        [Test]
        public void ExecuteCommand_WhenSuccessful_PushesUndoAndClearsRedo()
        {
            var history = new CommandHistory();
            var command = new FakeCommand(true, true);

            Assert.That(history.ExecuteCommand(command), Is.True);
            Assert.That(history.CanUndo(), Is.True);
            Assert.That(history.CanRedo(), Is.False);
            Assert.That(command.ExecuteCalls, Is.EqualTo(1));
        }

        [Test]
        public void UndoAndRedo_MoveCommandBetweenStacks()
        {
            var history = new CommandHistory();
            var command = new FakeCommand(true, true);

            Assert.That(history.ExecuteCommand(command), Is.True);
            Assert.That(history.Undo(), Is.True);
            Assert.That(history.CanUndo(), Is.False);
            Assert.That(history.CanRedo(), Is.True);
            Assert.That(command.UndoCalls, Is.EqualTo(1));

            Assert.That(history.Redo(), Is.True);
            Assert.That(history.CanUndo(), Is.True);
            Assert.That(history.CanRedo(), Is.False);
            Assert.That(command.ExecuteCalls, Is.EqualTo(2));
        }

        [Test]
        public void ExecuteCommand_WhenCommandFails_DoesNotAddToHistory()
        {
            var history = new CommandHistory();
            var command = new FakeCommand(false, true);

            Assert.That(history.ExecuteCommand(command), Is.False);
            Assert.That(history.CanUndo(), Is.False);
            Assert.That(history.CanRedo(), Is.False);
            Assert.That(command.ExecuteCalls, Is.EqualTo(1));
        }

        private sealed class FakeCommand : ICommand
        {
            private readonly bool _executeResult;
            private readonly bool _undoResult;

            public int ExecuteCalls { get; private set; }
            public int UndoCalls { get; private set; }

            public FakeCommand(bool executeResult, bool undoResult)
            {
                _executeResult = executeResult;
                _undoResult = undoResult;
            }

            public bool Execute()
            {
                ExecuteCalls++;
                return _executeResult;
            }

            public bool Undo()
            {
                UndoCalls++;
                return _undoResult;
            }

            public string Description => "FakeCommand";
        }
    }
}

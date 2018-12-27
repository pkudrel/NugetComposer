using System;
using System.Text;
using System.Threading;

namespace ChromeRuntimeDownloader.Vendors.ShellProgressBar
{
    public class ProgressBar : IDisposable, IProgress<double>
    {
        private const int _BLOCK_COUNT = 10;
        private const string _ANIMATION = @"|/-\";
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);

        private readonly Timer _timer;
        private int _animationIndex;

        private double _currentProgress;
        private string _currentText = string.Empty;
        private bool _disposed;

        public ProgressBar(string message)
        {
            Console.Write(message);
            _timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
                ResetTimer();
        }

        public void Dispose()
        {
            lock (_timer)
            {
                UpdateText(string.Empty);
                _disposed = true;
             
            }
        }

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref _currentProgress, value);
        }

        private void TimerHandler(object state)
        {
            lock (_timer)
            {
                if (_disposed) return;

                var progressBlockCount = (int) (_currentProgress * _BLOCK_COUNT);
                var percent = (int) (_currentProgress * 100);
                var text = string.Format("[{0}{1}] {2,3}% {3}",
                    new string('#', progressBlockCount), new string('-', _BLOCK_COUNT - progressBlockCount),
                    percent,
                    _ANIMATION[_animationIndex++ % _ANIMATION.Length]);
                UpdateText(text);

                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            // Get length of common portion
            var commonPrefixLength = 0;
            var commonLength = Math.Min(_currentText.Length, text.Length);
            while (commonPrefixLength < commonLength && text[commonPrefixLength] == _currentText[commonPrefixLength])
                commonPrefixLength++;

            // Backtrack to the first differing character
            var outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', _currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            var overlapCount = _currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            _currentText = text;
        }

        private void ResetTimer()
        {
            _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Finish()
        {
            _timer.Dispose();
            var end = @"[##########] 100%";
            UpdateText(end);
            Console.WriteLine(" - done");
        }
    }
}
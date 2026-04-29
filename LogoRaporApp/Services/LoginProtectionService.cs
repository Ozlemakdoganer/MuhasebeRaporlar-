namespace LogoRaporApp.Services
{
    public class LoginProtectionService
    {
        private static readonly Dictionary<string, (int Count, DateTime LastAttempt)> _attempts = new();
        private const int MaxAttempts = 3;
        private const int LockoutMinutes = 5;

        public bool IsLocked(string username)
        {
            if (!_attempts.ContainsKey(username)) return false;

            var (count, lastAttempt) = _attempts[username];

            if (count >= MaxAttempts && DateTime.Now - lastAttempt < TimeSpan.FromMinutes(LockoutMinutes))
                return true;

            if (DateTime.Now - lastAttempt >= TimeSpan.FromMinutes(LockoutMinutes))
                _attempts.Remove(username);

            return false;
        }

        public void RecordFailedAttempt(string username)
        {
            if (_attempts.ContainsKey(username))
            {
                var (count, _) = _attempts[username];
                _attempts[username] = (count + 1, DateTime.Now);
            }
            else
            {
                _attempts[username] = (1, DateTime.Now);
            }
        }

        public void ResetAttempts(string username)
        {
            _attempts.Remove(username);
        }

        public int RemainingMinutes(string username)
        {
            if (!_attempts.ContainsKey(username)) return 0;
            var (_, lastAttempt) = _attempts[username];
            var remaining = LockoutMinutes - (int)(DateTime.Now - lastAttempt).TotalMinutes;
            return remaining > 0 ? remaining : 0;
        }
    }
}
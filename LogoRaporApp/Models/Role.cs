namespace LogoRaporApp.Models
{
    public class Role
    {
        public string Name { get; set; } = "";
        public List<string> Permissions { get; set; } = new();
    }
}
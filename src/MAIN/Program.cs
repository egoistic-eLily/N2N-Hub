namespace N2N_USER_SERVER
{
    public class Program
    {
        static void Main()
        {
            Bootstrap.Initialization.Init();
            Bootstrap.HttpBootstrap.Setup();
        }
    }
}
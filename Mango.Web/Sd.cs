namespace Mango.Web;

public static class Sd
{
    public static string ProductApiBase { get; set; }
    public static string ShoppingCartApiBase { get; set; }
    
    public enum ApiType
    {
        Get,
        Post,
        Put,
        Delete
    }
}
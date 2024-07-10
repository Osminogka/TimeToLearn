namespace Authentication.DAL.Models;

public class ResponseMessage
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "Error occured";
}
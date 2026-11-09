using System.Text;
namespace RblxExecutorUI;
public static class ExecutionPayload
{
    public static byte[] Encode(string script) => Encoding.UTF8.GetBytes(script);
}

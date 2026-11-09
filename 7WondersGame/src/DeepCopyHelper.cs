using Newtonsoft.Json;

namespace _7WondersGame.src
{
    internal static class DeepCopyHelper
    {
        public static T? DeepCopy<T>(T obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                string json = JsonConvert.SerializeObject(obj);
                return JsonConvert.DeserializeObject<T>(json);
            }
        }
    }
}

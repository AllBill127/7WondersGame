using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NKeyLoggerLib;
public class Ref<T> where T : struct
{
    public T item { get; set; } = new T();
    public Ref(T item)
    {
        this.item = item;
    }
    public static Ref<T> operator << (Ref<T> left, T item)
    {
        return new Ref<T>(item);
    }
    public static Ref<T> create(T item)
    {
        return new Ref<T>(item);
    }
}



using System;
using System.Collections;

namespace RouletteDataCollector.Structs;

// dont look at this

// public class IndexableStruct
// {
//     public int Count()
//     {
//         return this.GetType().GetFields().Length;
//     }

//     public object? this[int index]
//     {
//         get
//         {
//             if (index < 0 || index > this.Count()) throw new IndexOutOfRangeException();
//             return this.GetType().GetFields()[index].GetValue(this);
//         }
//         set
//         {
//             if (index < 0 || index > this.Count()) throw new IndexOutOfRangeException();
//             this.GetType().GetFields()[index].SetValue(this, value);
//         }
//     }
// }

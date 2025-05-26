using System.Collections.Generic;

namespace GameplayAttributes {
public static class ListExtensions {
    public static void Swap<T>(this IList<T> list, int i, int j) {
        (list[i], list[j]) = (list[j], list[i]);
    }
}
}
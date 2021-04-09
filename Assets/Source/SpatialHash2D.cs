using System.Collections;
using Unity.Mathematics;

public class SpatialHash2D {
    private Hashtable idx;

    public SpatialHash2D() {
        idx = new Hashtable();
    }

    public int Count {
        get { return idx.Count; }
    }

    public ICollection Cells {
        get { return idx.Keys; }
    }

    public void Insert(int2 v, object obj) {
        ArrayList cell;
        string key = Key(v);

        if (idx.Contains(key))
            cell = (ArrayList)idx[key];
        else {
            cell = new ArrayList();
            idx.Add(key, cell);
        }
        if (!cell.Contains(obj))
            cell.Add(obj);
    }

    public ArrayList Query(int2 v) {
        string key = Key(v);
        if (idx.Contains(key))
            return (ArrayList)idx[key];
        return new ArrayList();
    }

    private string Key(int2 v) {
        return v.x.ToString() + ":" + v.y.ToString();
    }

}

using System.Collections;

namespace CliRelay.Collections;

public class ConcurrentList<T> : IList<T?>
{
    private ReaderWriterLockSlim _lock = new();
    private List<T?> _items = new List<T?>();

    private void ProtectedWrite(Action action)
    {
        try
        {
            _lock.EnterWriteLock();
            action();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    private TRet? ProtectedWriteWithRet<TRet>(Func<TRet> action)
    {
        try
        {
            _lock.EnterWriteLock();
            return action();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        
        return default;
    }
    
    private void ProtectedRead(Action action)
    {
        try
        {
            _lock.EnterReadLock();
            action();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    private TRet? ProtectedReadWithRet<TRet>(Func<TRet> action)
    {
        try
        {
            _lock.EnterReadLock();
            return action();
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            _lock.ExitReadLock();
        }
        
        return default;
    }

    public void Add(T? item) => ProtectedWrite(() => _items.Add(item));
    public void CopyTo(T?[] array, int arrayIndex) =>  ProtectedWrite(() => _items.CopyTo(array, arrayIndex));
    public bool Remove(T? item) => ProtectedWriteWithRet(() => _items.Remove(item));
    public int Count => ProtectedReadWithRet(() => _items.Count);
    public bool IsReadOnly => false;
    public int IndexOf(T? item) =>  ProtectedReadWithRet(() => _items.IndexOf(item));
    public void Insert(int index, T? item) =>  ProtectedWrite(() => _items.Insert(index, item));
    public void RemoveAt(int index) => ProtectedWrite(() => _items.RemoveAt(index));

    public T? this[int index]
    {
        get => ProtectedReadWithRet(() => _items[index]);
        set => ProtectedWrite(() => _items[index] = value);
    }

    public void Clear() => ProtectedWrite(() => _items.Clear());
    public bool Contains(T? item) => ProtectedReadWithRet(() => _items.Contains(item));
    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
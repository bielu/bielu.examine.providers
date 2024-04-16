namespace Bielu.Examine.Core.Services;

public class Unsubscriber<T> : IDisposable
{
    private List<IObserver<T>> _observers;
    private IObserver<T> _observer;

    public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
    {
        this._observers = observers;
        this._observer = observer;
    }

#pragma warning disable CA1816
    public void Dispose()
#pragma warning restore CA1816
    {
        if (! (_observer == null)) _observers.Remove(_observer);
    }
}

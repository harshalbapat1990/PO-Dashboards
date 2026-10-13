// using System;
using System.Collections;

namespace DesignAutomation.AppCatalog.Model;

/// <summary>Keeps a list of items and resets back to the first item automatically once the last item is reached</summary>
/// <typeparam name="T"></typeparam>
public class CircularList<T> : IEnumerable<T>
{
    
    /// <summary></summary>
    public CircularList( T item ) : this( [item] )
    {
    }

    /// <summary></summary>
    public CircularList( IEnumerable<T> items )
    {
        this.Items = new List<T>( items );
    }

    #region [ Properties ]

    /// <summary></summary>
    private List<T> Items { get; set; } = new List<T>();

    /// <summary>Get the first item</summary>
    public T Head => this.Items[0];

    /// <summary>Get the last item</summary>
    public T Current => this.Items[this.Index];

    /// <summary>Is the list empty</summary>
    public bool IsEmpty => this.Items.Count < 1;

    /// <summary>Is the list empty</summary>
    public int Size => this.Items.Count;

    /// <summary>Temporary counter that keeps track of how many items have been passed.</summary>
    private int Index { get; set; } = 0;

    #endregion [ Properties ]

    /// <summary>Inserts a new item after the position of the marker.  Sets the new item to point to the item that was after the marker before the insertion.</summary>
    public void Insert( T item )
    {
        this.Items.Insert( this.Index + 1, item );
    }

    /// <summary>Advance the marker one item ahead in the list.</summary>
    public void MoveNext()
    {
        if ( this.IsEmpty ) return;

        this.Index = ++this.Index % this.Items.Count;
    }

    /// <summary>Move the marker one item back in the list.</summary>
    public void MovePrevious()
    {
        if (this.IsEmpty) return;

        --this.Index;

        if ( this.Index < 0)
            this.Index = this.Items.Count - 1;
    }
	
	/// <summary>Moves to the item that matches the specified item</summary>
    public int Move( T item )
    {
		int idx = this.Items.IndexOf(item);

		if (idx > -1)
			this.Index = idx;

        return idx;
    }

    /// <summary>Resets the current marker to the first item in the list.</summary>
	public void Reset() => this.Index = 0;

    /// <summary></summary>
    /// <param name="action"></param>
    public void ForEach( Action<T> action )
    {
        this.Items.ForEach( action );
    }

    /// <summary>Get the item at index if possible</summary>
    public T At( int i )
    {
        if ( this.Items.Count < 1 || i > this.Items.Count - 1 )
            throw new ArgumentOutOfRangeException( "Exceeds size of the collection." );

        return this.Items[ i ];
    }

    /// <summary>Gets an enumerator that will touch all items from head to end</summary>
    public IEnumerator<T> GetEnumerator() => this.Items.GetEnumerator();

    /// <summary>Gets an enumerator that will touch all items from head to end</summary>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
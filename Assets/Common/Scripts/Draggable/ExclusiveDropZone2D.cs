namespace Common.Scripts.Draggable
{
    public class ExclusiveDropZone2D : DropZone2D
    {
        public Draggable2D occupant;

        public override void OnDrop(Draggable2D draggable, DropZone2D before)
        {
            base.OnDrop(draggable, before);

            if (occupant != null)
            {
                occupant.MoveToDropZone(before);
            }
            
            occupant = draggable;
        }

        public override void OnDragOut(Draggable2D item)
        {
            base.OnDragOut(item);

            if (item == occupant)
            {
                occupant = null;
            }
        }
    }
}
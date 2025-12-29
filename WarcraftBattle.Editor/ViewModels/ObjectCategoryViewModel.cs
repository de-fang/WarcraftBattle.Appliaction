namespace WarcraftBattle.Editor.ViewModels
{
    public class ObjectCategoryViewModel
    {
        public ObjectCategoryViewModel(string name, ObjectCategoryKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public string Name { get; }

        public ObjectCategoryKind Kind { get; }
    }
}

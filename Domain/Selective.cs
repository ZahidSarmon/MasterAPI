namespace Master.Domain;

public class Selective<T>
{
    public T Id { get; set; }
    public string Name { get; set; }
    public bool IsChecked { get; set; }
}

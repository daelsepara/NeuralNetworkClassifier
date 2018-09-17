public class Delimiter
{
	public string Name;
	public char Character;

	public Delimiter(string name, char character)
	{
		if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(character.ToString()))
		{
			Name = name;
			Character = character;
		}
		else
		{
			Name = "Comma ,";
			Character = ',';
		}
	}
}

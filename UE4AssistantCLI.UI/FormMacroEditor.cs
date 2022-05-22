using DynamicDescriptors;
using System.Drawing.Design;
using SystemEx;
using UE4Assistant;

namespace UE4AssistantCLI.UI;

public partial class FormMacroEditor : Form
{
	string str;
	Specifier specifier;
	DynamicTypeDescriptor so;

	public FormMacroEditor(string str)
	{
		this.str = str;

		InitializeComponent();
	}

	private void FormMacroEditor_Load(object sender, EventArgs e)
	{
		tabControlPages.ItemSize = new Size(0, 1);
		tabControlPages.SizeMode = TabSizeMode.Fixed;

		comboBoxMacro.Items.Clear();
		comboBoxMacro.Items.AddRange(SpecifierSchema.ReadAvailableTags().Cast<object>().ToArray());

		Specifier.TryParse(str.tokenize(), out specifier);

		comboBoxMacro.SelectedIndex = comboBoxMacro.Items.Cast<TagModel>().ToList().FindIndex(i => i.name == specifier.tag.name);

		var items = specifier.ToModelDictionary("parameters");
		var dso_items = items.ToDictionary(i => i.Key
			, i => i.Value.Length == 1
				? (specifier.data.GetValueOrDefault(i.Value[0].name, i.Value[0].DefaultValue), i.Value[0].Type)
				: (i.Value.Select(i => i.name).FirstOrDefault(i => specifier.data.ContainsKey(i), i.Value[0].name), typeof(string))
			);

		so = DynamicDescriptor.CreateFromDictionary(dso_items);

		foreach (var item in items)
		{
			var ri = item.Value[0];
			var dp = so.GetDynamicProperty(item.Key)
				.SetCategory(ri.category);

			if (ri.group.IsNullOrWhiteSpace())
			{
				if (ri.Type == typeof(bool))
					dp.SetEditor(typeof(UITypeEditor), new CheckboxBoolEditor());
			}
			else
			{
				dp.SetConverter(new StandardValuesStringConverter(item.Value.Select(i => i.name)));
			}
		}

		propertyGridSpecifier.SelectedObject = so;
	}

	private void comboBoxMacro_SelectedIndexChanged(object sender, EventArgs e)
	{

	}

	private void propertyGridSpecifier_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
	{
		var collection = specifier.model.collections["parameters"];
		foreach (var p in so.GetDynamicProperties())
		{
			var v = p.GetValue(so);
			var sp = collection.Find(mp => mp.name == p.Name);

			if (!sp.IsEmpty)
			{
				if (!Equals(v, sp.DefaultValue))
				{
					specifier.data[p.Name] = sp.type.IsNullOrWhiteSpace() ? null : v;
				}
				else
				{
					specifier.data.Remove(p.Name); 
				}
			}
			else
			{
				var group = collection.FindAll(mp => mp.group == p.Name);
				foreach (var i in group.Skip(1))
				{
					if (Equals(v, i.name))
					{
						specifier.data[i.name] = null;
					}
					else
					{
						specifier.data.Remove(i.name);
					}
				}
			}
		}

		textBoxResult.Text = specifier.ToString();
	}
}

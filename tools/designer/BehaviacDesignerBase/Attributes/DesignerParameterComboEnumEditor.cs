/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Tencent is pleased to support the open source community by making behaviac available.
//
// Copyright (C) 2015 THL A29 Limited, a Tencent company. All rights reserved.
//
// Licensed under the BSD 3-Clause License (the "License"); you may not use this file except in compliance with
// the License. You may obtain a copy of the License at http://opensource.org/licenses/BSD-3-Clause
//
// Unless required by applicable law or agreed to in writing, software distributed under the License is
// distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and limitations under the License.
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Reflection;
using System.Windows.Forms;
using Behaviac.Design.Properties;

namespace Behaviac.Design.Attributes
{
    public partial class DesignerParameterComboEnumEditor : Behaviac.Design.Attributes.DesignerPropertyEditor
    {
        public DesignerParameterComboEnumEditor()
        {
            InitializeComponent();

            _types.Add(VariableDef.kConst);
            _types.Add(VariableDef.kPar);
            _types.Add(VariableDef.kSelf);
            foreach (Plugin.InstanceName_t instanceName in Plugin.InstanceNames)
            {
                _types.Add(instanceName.displayName_);
            }
        }

        public override void ReadOnly()
        {
            base.ReadOnly();

            this.typeComboBox.Enabled = false;
            if (this.propertyEditor != null)
                this.propertyEditor.ReadOnly();
        }

        public override string DisplayName
        {
            get { return (_param != null) ? _param.DisplayName : base.DisplayName; }
        }

        public override string Description
        {
            get { return (_param != null) ? _param.Description : base.Description; }
        }

        private List<string> _types = new List<string>();

        public override void SetParameter(MethodDef.Param param, object obj)
        {
            base.SetParameter(param, obj);

            int typeIndex = -1;

            if (param.IsFromStruct)
            {
                string instance = string.Empty;
                string vt = VariableDef.kConst;
                if (_param.Value is ParInfo)
                {
                    vt = VariableDef.kPar;
                }
                else if (_param.Value is VariableDef)
                {
                    VariableDef v = _param.Value as VariableDef;
                    vt = v.ValueClass;

                    instance = vt;
                    if (instance != VariableDef.kSelf)
                    {
                        instance = Plugin.GetInstanceNameFromClassName(instance);
                    }
                }

                typeIndex = getComboIndex(vt, instance, "");

                setPropertyEditor(createEditor(vt));
            }
            else
            {
                string[] tokens = param.Value.ToString().Split(' ');
                string propertyName = tokens[tokens.Length - 1];
                string instance = Plugin.GetInstanceName(propertyName);
                string valueType = string.Empty;
                if (!string.IsNullOrEmpty(instance))
                {
                    propertyName = propertyName.Substring(instance.Length + 1, propertyName.Length - instance.Length - 1);
                    valueType = getValueType(instance, propertyName);
                }
                else
                {
                    valueType = getValueType(propertyName);
                }

                typeIndex = getComboIndex(valueType, instance, propertyName);

                setPropertyEditor(createEditor(valueType));
            }

            if (typeIndex > -1)
            {
                // Keep only one type for efficiency.
                this.typeComboBox.Items.Clear();
                this.typeComboBox.Items.Add(_types[typeIndex]);
                this.typeComboBox.SelectedIndex = 0;
            }
        }

        private void typeComboBox_DropDown(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(typeComboBox.Text))
            {
                foreach (string t in _types)
                {
                    if (!typeComboBox.Items.Contains(t))
                        typeComboBox.Items.Add(t);
                }
            }
            else
            {
                int index = -1;
                for (int i = 0; i < _types.Count; ++i)
                {
                    if (typeComboBox.Text == _types[i])
                    {
                        index = i;
                        break;
                    }
                }

                if (index > -1)
                {
                    for (int i = index - 1; i >= 0; --i)
                    {
                        if (!typeComboBox.Items.Contains(_types[i]))
                            typeComboBox.Items.Insert(0, _types[i]);
                    }

                    for (int i = index + 1; i < _types.Count; ++i)
                    {
                        if (!typeComboBox.Items.Contains(_types[i]))
                            typeComboBox.Items.Add(_types[i]);
                    }
                }
            }
        }

        private void setPropertyEditor(DesignerPropertyEditor editor)
        {
            this.propertyEditor = editor;

            if (this.propertyEditor != null)
            {
                this.propertyEditor.MouseEnter += typeComboBox_MouseEnter;
                this.propertyEditor.DescriptionWasChanged += propertyEditor_DescriptionWasChanged;
            }
        }

        private int getComboIndex(string valueType, string instanceName, string propertyName)
        {
            if (valueType == VariableDef.kConst)
                return 0;

            if (valueType == VariableDef.kPar)
                return 1;

            if (valueType == VariableDef.kSelf)
                return 2;

            if (string.IsNullOrEmpty(instanceName))
                instanceName = Plugin.GetClassName(propertyName);

            Debug.Check(!string.IsNullOrEmpty(instanceName));
            int index = Plugin.InstanceNameIndex(instanceName);
            Debug.Check(index >= 0);

            return index + 3;
        }

        private string getValueType(string instanceName, string propertyName)
        {
            if (instanceName == VariableDef.kSelf)
                return VariableDef.kSelf;

            AgentType agent = Plugin.GetInstanceAgentType(instanceName);
            if (agent != null)
            {
                IList<PropertyDef> properties = agent.GetProperties();
                foreach (PropertyDef p in properties)
                {
                    if (p.Name == propertyName
#if BEHAVIAC_NAMESPACE_FIX
                        || p.Name.EndsWith(propertyName)
#endif
                        )
                    {
                        return instanceName;
                    }
                }
            }

            return VariableDef.kConst;
        }

        private string getValueType(string propertyName)
        {
            Behaviac.Design.Nodes.Node node = _object as Behaviac.Design.Nodes.Node;
            Behaviac.Design.Nodes.Behavior behavior = (node != null) ? node.Behavior as Behaviac.Design.Nodes.Behavior : null;

            if (behavior != null)
            {
                // Try to find the Par parameter with the name.
                List<ParInfo> allPars = new List<ParInfo>();
                ((Nodes.Node)behavior).GetAllPars(ref allPars);
                if (allPars.Count > 0)
                {
                    foreach (ParInfo p in allPars)
                    {
                        if (p.Name == propertyName
#if BEHAVIAC_NAMESPACE_FIX
                            || p.Name.EndsWith(propertyName)
#endif
                            )
                            return VariableDef.kPar;
                    }
                }
            }

            // Try to find the Agent property with the name.
            if (behavior != null && behavior.AgentType != null)
            {
                IList<PropertyDef> properties = behavior.AgentType.GetProperties();
                foreach (PropertyDef p in properties)
                {
                    if (p.Name == propertyName
#if BEHAVIAC_NAMESPACE_FIX
                        || p.Name.EndsWith(propertyName)
#endif                        
                        )
                        return VariableDef.kSelf;
                }
            }

            // Try to find the World property with the name.
            string className = Plugin.GetClassName(propertyName);
            if (!string.IsNullOrEmpty(className))
            {
                AgentType agent = Plugin.GetInstanceAgentType(className);
                if (agent != null)
                {
                    IList<PropertyDef> properties = agent.GetProperties();
                    foreach (PropertyDef p in properties)
                    {
                        if (p.Name == propertyName
#if BEHAVIAC_NAMESPACE_FIX
                            || p.Name.EndsWith(propertyName)
#endif
                            )
                        {
                            return className;
                        }
                    }
                }
            }

            return VariableDef.kConst;
        }

        private Type getEditorType(string valueType)
        {
            if (valueType == VariableDef.kConst)
            {
                return (_param != null) ? _param.Attribute.GetEditorType(null) : null;
            }
            else if (valueType == VariableDef.kPar)
            {
                return typeof(DesignerParEnumEditor);
            }

            return typeof(DesignerPropertyEnumEditor);
        }

        private void setEditor(DesignerPropertyEditor editor, string valueType)
        {
            if (editor == null)
                return;

            if (valueType == VariableDef.kConst)
            {
                if (_param.Value != null && (_param.Value is VariableDef || _param.Value is PropertyDef || _param.Value is ParInfo))
                {
                    if (!(_param.IsFromStruct))
                    {
                        _param.Value = Plugin.DefaultValue(_param.Type);
                    }
                    else
                    {
                        if (_param.Value is VariableDef)
                        {
                            VariableDef v = _param.Value as VariableDef;

                            _param.Value = Plugin.DefaultValue(v.GetValueType());
                        }
                        else if (_param.Value is ParInfo)
                        {
                            ParInfo v = _param.Value as ParInfo;

                            _param.Value = Plugin.DefaultValue(v.Variable.GetValueType());
                        }
                    }
                }
            }
            else if (valueType == VariableDef.kPar)
            {
                if (_param.IsFromStruct)
                {
                    if (!(_param.Value is ParInfo) && !(_param.Value is VariableDef))
                    {
                        ParInfo par = new ParInfo(this._root);

                        par.Name = _param.Attribute.DisplayName;
                        par.TypeName = _param.Type.FullName;

                        par.Variable = new VariableDef(_param.Value, VariableDef.kPar);

                        _param.Value = par;
                    }
                    else
                    {
                        if (_param.Value is VariableDef)
                        {
                            VariableDef v = _param.Value as VariableDef;
                            if (v.ValueClass != valueType)
                            {
                                Type t1 = v.GetValueType() != null ? v.GetValueType() : _param.Type;
                                object dv = Plugin.DefaultValue(t1);
                                _param.Value = new VariableDef(dv, valueType);
                            }
                        }
                    }
                }
            }
            else
            {
                if (!_param.IsFromStruct)
                {
                    if (valueType == VariableDef.kSelf)
                    {
                        DesignerPropertyEnumEditor propertyEnumEditor = editor as DesignerPropertyEnumEditor;
                        propertyEnumEditor.GlobalType = null;
                        propertyEnumEditor.FilterType = _param.Type;
                    }
                    else
                    {
                        DesignerPropertyEnumEditor propertyEnumEditor = editor as DesignerPropertyEnumEditor;
                        propertyEnumEditor.GlobalType = valueType;
                        propertyEnumEditor.FilterType = _param.Type;
                    }
                }
                else
                {
                    DesignerPropertyEnumEditor propertyEnumEditor = editor as DesignerPropertyEnumEditor;

                    if (valueType == VariableDef.kSelf)
                    {
                        propertyEnumEditor.GlobalType = null;
                    }
                    else
                    {
                        propertyEnumEditor.GlobalType = valueType;
                    }

                    if (_param.Value is VariableDef)
                    {
                        VariableDef v = _param.Value as VariableDef;
                        if (v.ValueClass != valueType)
                        {
                            Type t1 = v.GetValueType() != null ? v.GetValueType() : _param.Type;
                            object dv = Plugin.DefaultValue(t1);
                            _param.Value = new VariableDef(dv, valueType);
                        }

                        propertyEnumEditor.FilterType = (v.GetValueType() != null ? v.GetValueType() : _param.Type);
                    }
                    else if (_param.Value is ParInfo)
                    {
                        ParInfo v = _param.Value as ParInfo;
                        if (v.Variable.ValueClass != valueType)
                        {
                            object dv = Plugin.DefaultValue(v.Variable.GetValueType());
                            _param.Value = new VariableDef(dv, valueType);
                        }

                        propertyEnumEditor.FilterType = v.Variable.GetValueType();
                    }
                    else
                    {
                        _param.Value = new VariableDef(_param.Value, valueType);
                        propertyEnumEditor.FilterType = _param.Type;
                    }
                }
            }

            editor.SetParameter(_param, _object);

            editor.ValueWasAssigned();
            editor.ValueWasChanged += editor_ValueWasChanged;
        }

        private void editor_ValueWasChanged(object sender, DesignerPropertyInfo property)
        {
            OnValueChanged(_property);
        }

        private DesignerPropertyEditor createEditor(string valueType)
        {
            if (flowLayoutPanel.Controls.Count > 1)
                flowLayoutPanel.Controls.RemoveAt(1);

            Type editorType = getEditorType(valueType);
            if (editorType == null)
                return null;

            DesignerPropertyEditor editor = (DesignerPropertyEditor)editorType.InvokeMember(string.Empty, BindingFlags.CreateInstance, null, null, new object[0]);
            editor.Location = new System.Drawing.Point(74, 1);
            editor.Margin = new System.Windows.Forms.Padding(0);
            editor.Size = new System.Drawing.Size(flowLayoutPanel.Width - this.typeComboBox.Width - 5, 20);
            editor.TabIndex = 1;

            setEditor(editor, valueType);

            flowLayoutPanel.Controls.Add(editor);
            return editor;
        }

        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.typeComboBox.SelectedItem != null)
            {
                setPropertyEditor(createEditor((string)this.typeComboBox.SelectedItem));

                OnValueChanged(_property);
            }
        }

        private void flowLayoutPanel_Resize(object sender, EventArgs e)
        {
            if (this.propertyEditor != null)
                this.propertyEditor.Width = flowLayoutPanel.Width - this.typeComboBox.Width - 5;
        }

        private void typeComboBox_MouseEnter(object sender, EventArgs e)
        {
            this.OnMouseEnter(e);
        }

        private void propertyEditor_DescriptionWasChanged(string displayName, string description)
        {
            this.OnDescriptionChanged(displayName, description);
        }
    }
}

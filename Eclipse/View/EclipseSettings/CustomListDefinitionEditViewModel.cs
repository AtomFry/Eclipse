using Eclipse.Event;
using Eclipse.Helpers;
using Eclipse.Models;
using Eclipse.Service;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace Eclipse.View.EclipseSettings
{
    public class CustomListDefinitionEditViewModel : ViewModelBase
    {
        private readonly CustomListDefinitionDataProvider eclipseSettingsDataProvider;
        private readonly IEventAggregator eventAggregator;
        private readonly CustomListDefinition customListDefinition;

        private FilterExpression selectedFilterExpression;
        private SortExpression selectedSortExpression;
        private ListCategoryType selectedListCategoryType;
        private ListCategoryType selectedRemainingListCategoryType;

        public ICommand AddFilterExpressionCommand { get; }
        public ICommand RemoveFilterExpressionCommand { get; }
        public ICommand AddSortExpressionCommand { get; }
        public ICommand RemoveSortExpressionCommand { get; }
        public ICommand MoveUpSortExpressionCommand { get; }
        public ICommand MoveDownSortExpressionCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CloseCommand { get; }

        public ICommand AddAllListCategoryTypesCommand { get; }
        public ICommand AddListCategoryTypeCommand { get; }
        public ICommand RemoveListCategoryTypeCommand { get; }
        public ICommand RemoveAllListCategoryTypesCommand { get; }

        public ObservableCollection<ListCategoryType> ListCategoryTypes { get; set; }
        public ObservableCollection<ListCategoryType> RemainingListCategoryTypes { get; set; }

        public ObservableCollection<FilterExpression> FilterExpressions { get; set; }
        public ObservableCollection<SortExpression> SortExpressions { get; set; }

        public CustomListDefinitionEditViewModel(CustomListDefinition customList)
        {
            customListDefinition = customList;

            eventAggregator = EventAggregatorHelper.Instance.EventAggregator;
            eclipseSettingsDataProvider = new CustomListDefinitionDataProvider();

            InitializeFilterExpressions();
            InitializeSortExpressions();
            InitializeListCategoryTypes();

            AddFilterExpressionCommand = new DelegateCommand(OnAddFilterExpressionExecute);
            RemoveFilterExpressionCommand = new DelegateCommand(OnRemoveFilterExpressionExecute);
            AddSortExpressionCommand = new DelegateCommand(OnAddSortExpressionExecute);
            RemoveSortExpressionCommand = new DelegateCommand(OnRemoveSortExpressionExecute);
            MoveUpSortExpressionCommand = new DelegateCommand(OnMoveUpSortExpressionExecute);
            MoveDownSortExpressionCommand = new DelegateCommand(OnMoveDownSortExpressionExecute);

            SaveCommand = new DelegateCommand(OnSaveExecuteAsync);
            CloseCommand = new DelegateCommand(OnCloseExecute);

            AddAllListCategoryTypesCommand = new DelegateCommand(OnAddAllListCategoryTypesExecute);
            AddListCategoryTypeCommand = new DelegateCommand(OnAddListCategoryTypeExecute);
            RemoveListCategoryTypeCommand = new DelegateCommand(OnRemoveListCategoryTypeExecute);
            RemoveAllListCategoryTypesCommand = new DelegateCommand(OnRemoveAllListCategoryTypesExecute);

        }

        public string Id
        {
            get { return customListDefinition.Id; }
            set
            {
                customListDefinition.Id = value;
                OnPropertyChanged("Id");
            }
        }

        public string Description
        {
            get { return customListDefinition.Description; }
            set
            {
                customListDefinition.Description = value;
                OnPropertyChanged("Description");
            }
        }

        public int MaxGamesInList
        {
            get { return customListDefinition.MaxGamesInList; }
            set
            {
                customListDefinition.MaxGamesInList = value;
                OnPropertyChanged("MaxGamesInList");
            }
        }

        private void OnCloseExecute()
        {
            eventAggregator.GetEvent<CustomListDefinitionEditClose>().Publish();
        }

        private async void OnSaveExecuteAsync()
        {
            bool isValid = true;
            StringBuilder stringBuilder = new StringBuilder("The following fields contain invalid values: ");
            stringBuilder.AppendLine();

            // convert selected field values to their native type
            foreach (FilterExpression filterExpression in FilterExpressions)
            {
                // is null and is not null operators don't need/have a value to convert 
                if (filterExpression.FilterFieldOperator == FilterFieldOperator.IsNotNull ||
                    filterExpression.FilterFieldOperator == FilterFieldOperator.IsNull)
                {
                    continue;
                }

                string val = filterExpression.FilterFieldValue.ToString();

                if (!filterExpression.GameFieldEnum.IsFilterFieldOperatorValidForField(filterExpression.FilterFieldOperator))
                {
                    isValid = false;
                    stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the operator is not valid for the field");
                }

                switch (filterExpression.GameFieldEnum.ToGameFieldType())
                {
                    case GameFieldType.Bool:
                        bool boolValue;

                        if (bool.TryParse(val, out boolValue))
                        {
                            filterExpression.FilterFieldValue = boolValue;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be 'true' or 'false'");
                        }

                        break;

                    case GameFieldType.DateTime:
                        DateTime dateTimeValue = DateTime.MinValue;

                        if (DateTime.TryParse(val, out dateTimeValue))
                        {
                            filterExpression.FilterFieldValue = dateTimeValue;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be date with format YYYY-MM-DD");
                        }
                        break;

                    case GameFieldType.DateTimeNullable:
                        DateTime date;

                        if (string.IsNullOrWhiteSpace(val))
                        {
                            filterExpression.FilterFieldValue = null;
                        }
                        else if (DateTime.TryParse(val, out date))
                        {
                            filterExpression.FilterFieldValue = date;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be blank or a date with format YYYY-MM-DD");
                        }
                        break;

                    case GameFieldType.Float:
                        float floatValue = float.MinValue;
                        if (float.TryParse(val, out floatValue))
                        {
                            filterExpression.FilterFieldValue = floatValue;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be numeric");
                        }
                        break;

                    case GameFieldType.Int:
                        int intValue = int.MinValue;

                        if (int.TryParse(val, out intValue))
                        {
                            filterExpression.FilterFieldValue = intValue;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be a whole number");
                        }
                        break;

                    case GameFieldType.IntNullable:
                        int nullableIntValue = int.MinValue;

                        if (string.IsNullOrWhiteSpace(val))
                        {
                            filterExpression.FilterFieldValue = null;
                        }
                        else if (int.TryParse(val, out nullableIntValue))
                        {
                            filterExpression.FilterFieldValue = nullableIntValue;
                        }
                        else
                        {
                            isValid = false;
                            stringBuilder.AppendLine($"{filterExpression.GameFieldEnum} {filterExpression.FilterFieldOperator} {filterExpression.FilterFieldValue}, the value must be either blank or a whole number");
                        }
                        break;
                    default:
                        break;
                }
            }

            if (isValid)
            {
                await eclipseSettingsDataProvider.SaveCustomListDefinitionAsync(customListDefinition);
                eventAggregator.GetEvent<CustomListDefinitionSaved>().Publish(customListDefinition.Id);
                eventAggregator.GetEvent<CustomListDefinitionEditClose>().Publish();
            }
            else
            {
                MessageDialogHelper.ShowOKDialog(stringBuilder.ToString(), "Invalid filters");
            }
        }

        private void OnMoveDownSortExpressionExecute()
        {
            if (SelectedSortExpression == null)
            {
                return;
            }

            SortExpression currentSortExpression = SelectedSortExpression;

            int selectedIndex = SortExpressions.IndexOf(currentSortExpression);
            if (selectedIndex + 1 < SortExpressions.Count)
            {
                SortExpressions.RemoveAt(selectedIndex);
                SortExpressions.Insert(selectedIndex + 1, currentSortExpression);
            }

            SelectedSortExpression = currentSortExpression;
        }

        private void OnMoveUpSortExpressionExecute()
        {
            if (SelectedSortExpression == null)
            {
                return;
            }

            SortExpression currentSortExpression = SelectedSortExpression;

            int selectedIndex = SortExpressions.IndexOf(currentSortExpression);
            if (selectedIndex - 1 >= 0)
            {
                SortExpressions.RemoveAt(selectedIndex);
                SortExpressions.Insert(selectedIndex - 1, currentSortExpression);
            }

            SelectedSortExpression = currentSortExpression;
        }

        private void OnRemoveSortExpressionExecute()
        {
            if (SelectedSortExpression != null)
            {
                SortExpressions.Remove(SelectedSortExpression);
                SelectedSortExpression = null;
                OnPropertyChanged("SelectedSortExpression");
            }
        }

        private void OnAddSortExpressionExecute()
        {
            SortExpressions.Add(new SortExpression());
            SelectedSortExpression = null;
            OnPropertyChanged("SelectedSortExpression");
        }

        private void OnRemoveFilterExpressionExecute()
        {
            if (SelectedFilterExpression != null)
            {
                FilterExpressions.Remove(SelectedFilterExpression);
                SelectedFilterExpression = null;
                OnPropertyChanged("SelectedFilterExpression");
            }
        }

        private void OnAddFilterExpressionExecute()
        {
            FilterExpressions.Add(new FilterExpression());
            SelectedFilterExpression = null;
            OnPropertyChanged("SelectedFilterExpression");
        }

        private void InitializeFilterExpressions()
        {
            FilterExpressions = new ObservableCollection<FilterExpression>();
            foreach (FilterExpression filterExpression in customListDefinition.FilterExpressions)
            {
                FilterExpressions.Add(filterExpression);
            }
            FilterExpressions.CollectionChanged += FilterExpressions_CollectionChanged;
        }

        private void InitializeSortExpressions()
        {
            SortExpressions = new ObservableCollection<SortExpression>();
            foreach (SortExpression sortExpression in customListDefinition.SortExpressions)
            {
                SortExpressions.Add(sortExpression);
            }
            SortExpressions.CollectionChanged += SortExpressions_CollectionChanged;
        }

        private void InitializeListCategoryTypes()
        {
            ListCategoryTypes = new ObservableCollection<ListCategoryType>();
            RemainingListCategoryTypes = new ObservableCollection<ListCategoryType>();

            foreach (ListCategoryType listCategoryType in customListDefinition.ListCategoryTypes)
            {
                ListCategoryTypes.Add(listCategoryType);
            }

            foreach (ListCategoryType listCategoryType in Enum.GetValues(typeof(ListCategoryType)))
            {
                if (!listCategoryType.IsValidForCustomList())
                {
                    continue;
                }

                if (customListDefinition.ListCategoryTypes.Contains(listCategoryType))
                {
                    continue;
                }

                RemainingListCategoryTypes.Add(listCategoryType);
            }

            ListCategoryTypes.CollectionChanged += SelectedListGroups_CollectionChanged;
        }

        private void OnRemoveAllListCategoryTypesExecute()
        {
            List<ListCategoryType> categoryTypes = new List<ListCategoryType>();
            foreach (ListCategoryType listCategoryType in ListCategoryTypes)
            {
                categoryTypes.Add(listCategoryType);
            }

            foreach (ListCategoryType listCategoryType in categoryTypes)
            {
                RemainingListCategoryTypes.Add(listCategoryType);
                ListCategoryTypes.Remove(listCategoryType);
                SelectedRemainingListCategoryType = listCategoryType;
            }
        }

        private void OnRemoveListCategoryTypeExecute()
        {
            if (!SelectedListCategoryType.IsValidForCustomList())
            {
                return;
            }

            if (RemainingListCategoryTypes.Contains(SelectedListCategoryType))
            {
                return;
            }

            int currentIndex = ListCategoryTypes.IndexOf(SelectedListCategoryType);

            RemainingListCategoryTypes.Add(SelectedListCategoryType);
            ListCategoryTypes.Remove(SelectedListCategoryType);
            SelectedRemainingListCategoryType = SelectedListCategoryType;

            if (ListCategoryTypes.Count > 0)
            {
                if (ListCategoryTypes.Count - 1 < currentIndex)
                {
                    currentIndex = ListCategoryTypes.Count - 1;
                }
                SelectedListCategoryType = ListCategoryTypes[currentIndex];
            }
        }

        private void OnAddListCategoryTypeExecute()
        {
            if (!SelectedRemainingListCategoryType.IsValidForCustomList())
            {
                return;
            }

            if (ListCategoryTypes.Contains(SelectedRemainingListCategoryType))
            {
                return;
            }

            int currentIndex = RemainingListCategoryTypes.IndexOf(SelectedRemainingListCategoryType);

            ListCategoryTypes.Add(SelectedRemainingListCategoryType);
            RemainingListCategoryTypes.Remove(SelectedRemainingListCategoryType);
            SelectedListCategoryType = SelectedRemainingListCategoryType;

            if (RemainingListCategoryTypes.Count > 0)
            {
                if (RemainingListCategoryTypes.Count - 1 < currentIndex)
                {
                    currentIndex = RemainingListCategoryTypes.Count - 1;
                }
                SelectedRemainingListCategoryType = RemainingListCategoryTypes[currentIndex];
            }
        }

        private void OnAddAllListCategoryTypesExecute()
        {
            List<ListCategoryType> categoryTypes = new List<ListCategoryType>();
            foreach (ListCategoryType listCategoryType in RemainingListCategoryTypes)
            {
                categoryTypes.Add(listCategoryType);
            }

            foreach (ListCategoryType listCategoryType in categoryTypes)
            {
                ListCategoryTypes.Add(listCategoryType);
                RemainingListCategoryTypes.Remove(listCategoryType);
                SelectedListCategoryType = listCategoryType;
            }
        }


        private void SelectedListGroups_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            customListDefinition.ListCategoryTypes.Clear();
            foreach (ListCategoryType listCategoryType in ListCategoryTypes)
            {
                customListDefinition.ListCategoryTypes.Add(listCategoryType);
            }
        }

        private void FilterExpressions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            customListDefinition.FilterExpressions.Clear();
            foreach (FilterExpression filterExpression in FilterExpressions)
            {
                customListDefinition.FilterExpressions.Add(filterExpression);
            }
        }

        private void SortExpressions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            customListDefinition.SortExpressions.Clear();
            foreach (SortExpression sortExpression in SortExpressions)
            {
                customListDefinition.SortExpressions.Add(sortExpression);
            }
        }

        public SortExpression SelectedSortExpression
        {
            get { return selectedSortExpression; }
            set
            {
                selectedSortExpression = value;
                OnPropertyChanged("SelectedSortExpression");
                InvalidateCommands();
            }
        }

        public FilterExpression SelectedFilterExpression
        {
            get { return selectedFilterExpression; }
            set
            {
                selectedFilterExpression = value;
                OnPropertyChanged("SelectedFilterExpression");
                InvalidateCommands();
            }
        }

        public ListCategoryType SelectedListCategoryType
        {
            get { return selectedListCategoryType; }
            set
            {
                selectedListCategoryType = value;
                OnPropertyChanged("SelectedListCategoryType");
                InvalidateCommands();
            }
        }

        public ListCategoryType SelectedRemainingListCategoryType
        {
            get { return selectedRemainingListCategoryType; }
            set
            {
                selectedRemainingListCategoryType = value;
                OnPropertyChanged("SelectedRemainingListCategoryType");
                InvalidateCommands();
            }
        }


        private void InvalidateCommands()
        {
            ((DelegateCommand)SaveCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)RemoveFilterExpressionCommand).RaiseCanExecuteChanged();
            ((DelegateCommand)RemoveSortExpressionCommand).RaiseCanExecuteChanged();
        }

        public Uri IconUri { get; } = ResourceImages.EclipseSettingsIcon1;
    }
}
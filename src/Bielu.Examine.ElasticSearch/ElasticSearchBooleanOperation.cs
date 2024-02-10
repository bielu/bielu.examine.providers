using Bielu.Examine.ElasticSearch.Queries;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Search;

namespace Bielu.Examine.Elasticsearch2;

public class ElasticSearchBooleanOperation : LuceneBooleanOperationBase
    {
        private readonly ElasticSearchQuery _search;
        internal ElasticSearchBooleanOperation(ElasticSearchQuery search)
            : base(search)
        {

            _search = search;
        }

        #region IBooleanOperation Members
        protected override INestedQuery AndNested() => new ElasticQuery(this._search, Occur.MUST);

        /// <inheritdoc />
        protected override INestedQuery OrNested() => new ElasticQuery(this._search, Occur.SHOULD);

        /// <inheritdoc />
        protected override INestedQuery NotNested() => new ElasticQuery(this._search, Occur.MUST_NOT);

        /// <inheritdoc />
        public override IQuery And() => new ElasticQuery(this._search, Occur.MUST);


        /// <inheritdoc />
        public override IQuery Or() => new ElasticQuery(this._search, Occur.SHOULD);


        /// <inheritdoc />
        public override IQuery Not() => new ElasticQuery(this._search, Occur.MUST_NOT);

        #endregion

        #region IOrdering

        public override ISearchResults Execute(QueryOptions? options = null)
        {
         return   _search.Execute(options);
        }

        public override IOrdering OrderBy(params SortableField[] fields) => _search.OrderBy(fields);

        public override IOrdering OrderByDescending(params SortableField[] fields) => _search.OrderByDescending(fields);

        #endregion
        #region Select Fields


        public override IOrdering SelectFields(ISet<string> fieldNames) => _search.SelectFieldsInternal(fieldNames);

        public override IOrdering SelectField(string fieldName) => _search.SelectFieldInternal(fieldName);


        public override IOrdering SelectAllFields() => _search.SelectAllFieldsInternal();

        #endregion
        public override string ToString() => _search.ToString();
        protected internal new LuceneBooleanOperationBase Op(
            Func<INestedQuery, INestedBooleanOperation> inner,
            BooleanOperation outerOp,
            BooleanOperation? defaultInnerOp = null)
        {
            this._search.____RULE_VIOLATION____Queries____RULE_VIOLATION____.Push(new BooleanQuery());
            BooleanOperation booleanOperation1 = this._search.BooleanOperation;
            if (defaultInnerOp.HasValue)
                this._search.BooleanOperation = defaultInnerOp.Value;
            INestedBooleanOperation booleanOperation2 = inner((INestedQuery) this._search);
            if (defaultInnerOp.HasValue)
                this._search.BooleanOperation = booleanOperation1;
            return this._search.LuceneQuery((Query) this._search.____RULE_VIOLATION____Queries____RULE_VIOLATION____.Pop(), new BooleanOperation?(outerOp));
        }

    }

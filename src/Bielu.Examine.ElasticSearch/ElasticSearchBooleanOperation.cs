using Bielu.Examine.Core.Queries;
using Bielu.Examine.Elasticsearch.Queries;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Search;
using ElasticSearchQuery = Bielu.Examine.Elasticsearch.Queries.ElasticSearchQuery;

namespace Bielu.Examine.Elasticsearch;

public class ElasticSearchBooleanOperation(ElasticSearchQuery search) : BieluExamineBooleanOperation(search)
    {


        #region IBooleanOperation Members
        protected override INestedQuery AndNested() => new ElasticQuery(search, Occur.MUST);

        /// <inheritdoc />
        protected override INestedQuery OrNested() => new ElasticQuery(search, Occur.SHOULD);

        /// <inheritdoc />
        protected override INestedQuery NotNested() => new ElasticQuery(search, Occur.MUST_NOT);

        /// <inheritdoc />
        public override IQuery And() => new ElasticQuery(search, Occur.MUST);


        /// <inheritdoc />
        public override IQuery Or() => new ElasticQuery(search, Occur.SHOULD);


        /// <inheritdoc />
        public override IQuery Not() => new ElasticQuery(search, Occur.MUST_NOT);

        #endregion

        protected internal new LuceneBooleanOperationBase Op(
            Func<INestedQuery, INestedBooleanOperation> inner,
            BooleanOperation outerOp,
            BooleanOperation? defaultInnerOp = null)
        {
            search.____RULE_VIOLATION____Queries____RULE_VIOLATION____.Push(new BooleanQuery());
            BooleanOperation booleanOperation1 = search.BooleanOperation;
            if (defaultInnerOp.HasValue)
                search.BooleanOperation = defaultInnerOp.Value;
            INestedBooleanOperation booleanOperation2 = inner((INestedQuery) search);
            if (defaultInnerOp.HasValue)
                search.BooleanOperation = booleanOperation1;
            return search.LuceneQuery((Query) search.____RULE_VIOLATION____Queries____RULE_VIOLATION____.Pop(), new BooleanOperation?(outerOp));
        }

    }

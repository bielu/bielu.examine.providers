using Bielu.Examine.Core.Queries;
using Examine;
using Examine.Lucene.Search;
using Examine.Search;
using Lucene.Net.Search;

namespace Bielu.Examine.Elasticsearch;

public class BieluExamineBooleanOperation(BieluExamineQuery search) : BieluExamineBooleanOperationBase(search)
    {


        #region IBooleanOperation Members
        protected override INestedQuery AndNested() => new ExamineQuery(search, Occur.MUST);

        /// <inheritdoc />
        protected override INestedQuery OrNested() => new ExamineQuery(search, Occur.SHOULD);

        /// <inheritdoc />
        protected override INestedQuery NotNested() => new ExamineQuery(search, Occur.MUST_NOT);

        /// <inheritdoc />
        public override IQuery And() => new ExamineQuery(search, Occur.MUST);


        /// <inheritdoc />
        public override IQuery Or() => new ExamineQuery(search, Occur.SHOULD);


        /// <inheritdoc />
        public override IQuery Not() => new ExamineQuery(search, Occur.MUST_NOT);

        #endregion

        protected internal new LuceneBooleanOperationBase Op(
            Func<INestedQuery, INestedBooleanOperation> inner,
            BooleanOperation outerOp,
            BooleanOperation? defaultInnerOp = null)
        {
            search.Queries.Push(new BooleanQuery());
            BooleanOperation booleanOperation1 = search.BooleanOperation;
            if (defaultInnerOp.HasValue)
                search.BooleanOperation = defaultInnerOp.Value;
            INestedBooleanOperation booleanOperation2 = inner((INestedQuery) search);
            if (defaultInnerOp.HasValue)
                search.BooleanOperation = booleanOperation1;
            return search.LuceneQuery((Query) search.Queries.Pop(), new BooleanOperation?(outerOp));
        }

    }

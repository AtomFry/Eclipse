using Eclipse.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Eclipse.Service
{
    // Every field a custom list can filter or sort on, as a compile-checked expression.
    //
    // This replaces looking properties up by name at runtime (B-15b). The old path took the
    // string from GameFieldEnum.ToFieldName - "Game.StarRatingFloat" - and walked it with
    // reflection, so renaming a property broke the user's custom lists silently, at runtime,
    // in a file that lives in their LaunchBox folder rather than anywhere in this repository.
    // Renaming one now stops the build.
    //
    // The expressions are kept as expressions rather than compiled delegates for two reasons:
    // the filter is assembled from the body, exactly as before, so comparison and conversion
    // behave identically down to which combinations throw; and the sort hands the whole
    // expression to Queryable, so ordering still uses Comparer<T>.Default for the property's
    // real type rather than boxing every key to object.
    public static class GameFields
    {
        private static readonly Dictionary<GameFieldEnum, IGameFieldAccessor> Accessors =
            new Dictionary<GameFieldEnum, IGameFieldAccessor>
            {
                [GameFieldEnum.Broken] = Field(gameMatch => gameMatch.Game.Broken),
                [GameFieldEnum.CommunityOrLocalStarRating] = Field(gameMatch => gameMatch.Game.CommunityOrLocalStarRating),
                [GameFieldEnum.CommunityStarRating] = Field(gameMatch => gameMatch.Game.CommunityStarRating),
                [GameFieldEnum.CommunityStarRatingTotalVotes] = Field(gameMatch => gameMatch.Game.CommunityStarRatingTotalVotes),
                [GameFieldEnum.DateAdded] = Field(gameMatch => gameMatch.Game.DateAdded),
                [GameFieldEnum.DateModified] = Field(gameMatch => gameMatch.Game.DateModified),
                [GameFieldEnum.Developers] = Field(gameMatch => gameMatch.Game.Developer),
                [GameFieldEnum.Favorite] = Field(gameMatch => gameMatch.Game.Favorite),
                [GameFieldEnum.Genres] = Field(gameMatch => gameMatch.Game.GenresString),
                [GameFieldEnum.Hide] = Field(gameMatch => gameMatch.Game.Hide),
                [GameFieldEnum.LastPlayedDate] = Field(gameMatch => gameMatch.Game.LastPlayedDate),
                [GameFieldEnum.Platform] = Field(gameMatch => gameMatch.Game.Platform),
                [GameFieldEnum.PlayCount] = Field(gameMatch => gameMatch.Game.PlayCount),
                [GameFieldEnum.PlayModes] = Field(gameMatch => gameMatch.Game.PlayMode),
                [GameFieldEnum.Publishers] = Field(gameMatch => gameMatch.Game.Publisher),
                [GameFieldEnum.Rating] = Field(gameMatch => gameMatch.Game.Rating),
                [GameFieldEnum.Region] = Field(gameMatch => gameMatch.Game.Region),
                [GameFieldEnum.ReleaseDate] = Field(gameMatch => gameMatch.Game.ReleaseDate),
                [GameFieldEnum.ReleaseYear] = Field(gameMatch => gameMatch.Game.ReleaseYear),
                [GameFieldEnum.Series] = Field(gameMatch => gameMatch.Game.Series),
                [GameFieldEnum.SortTitle] = Field(gameMatch => gameMatch.Game.SortTitle),
                [GameFieldEnum.SortTitleOrTitle] = Field(gameMatch => gameMatch.Game.SortTitleOrTitle),
                [GameFieldEnum.Source] = Field(gameMatch => gameMatch.Game.Source),
                [GameFieldEnum.StarRating] = Field(gameMatch => gameMatch.Game.StarRatingFloat),
                [GameFieldEnum.Status] = Field(gameMatch => gameMatch.Game.Status),
                [GameFieldEnum.Title] = Field(gameMatch => gameMatch.Game.Title),
                [GameFieldEnum.Version] = Field(gameMatch => gameMatch.Game.Version)
            };

        /// <summary>
        /// The fields with no accessor, if any. GameFieldEnum, ToFieldName and this table all
        /// enumerate the same set by hand, so they can drift apart; this is how that drift is
        /// noticed rather than surfacing as a broken custom list.
        /// </summary>
        public static IEnumerable<GameFieldEnum> UnmappedFields()
        {
            return Enum.GetValues(typeof(GameFieldEnum))
                       .Cast<GameFieldEnum>()
                       .Where(field => !Accessors.ContainsKey(field));
        }

        public static IGameFieldAccessor For(GameFieldEnum gameFieldEnum)
        {
            IGameFieldAccessor accessor;
            if (Accessors.TryGetValue(gameFieldEnum, out accessor))
            {
                return accessor;
            }

            throw new NotSupportedException($"No game field accessor is defined for {gameFieldEnum}.");
        }

        // The value type is inferred from the expression, so each entry above states the
        // property and nothing else - and states it in a form the compiler checks.
        private static IGameFieldAccessor Field<TValue>(Expression<Func<GameMatch, TValue>> expression)
        {
            return new GameFieldAccessor<TValue>(expression);
        }
    }

    // A field's expression, plus the four ordering operations that need its value type. The
    // interface is non-generic so the table can hold fields of different types; the four
    // methods mirror the four Queryable calls the old reflection path selected by name.
    public interface IGameFieldAccessor
    {
        LambdaExpression Expression { get; }

        IOrderedQueryable<GameMatch> OrderBy(IQueryable<GameMatch> source);
        IOrderedQueryable<GameMatch> OrderByDescending(IQueryable<GameMatch> source);
        IOrderedQueryable<GameMatch> ThenBy(IOrderedQueryable<GameMatch> source);
        IOrderedQueryable<GameMatch> ThenByDescending(IOrderedQueryable<GameMatch> source);
    }

    internal sealed class GameFieldAccessor<TValue> : IGameFieldAccessor
    {
        private readonly Expression<Func<GameMatch, TValue>> expression;

        public GameFieldAccessor(Expression<Func<GameMatch, TValue>> expression)
        {
            this.expression = expression;
        }

        public LambdaExpression Expression => expression;

        public IOrderedQueryable<GameMatch> OrderBy(IQueryable<GameMatch> source)
        {
            return Queryable.OrderBy(source, expression);
        }

        public IOrderedQueryable<GameMatch> OrderByDescending(IQueryable<GameMatch> source)
        {
            return Queryable.OrderByDescending(source, expression);
        }

        public IOrderedQueryable<GameMatch> ThenBy(IOrderedQueryable<GameMatch> source)
        {
            return Queryable.ThenBy(source, expression);
        }

        public IOrderedQueryable<GameMatch> ThenByDescending(IOrderedQueryable<GameMatch> source)
        {
            return Queryable.ThenByDescending(source, expression);
        }
    }
}

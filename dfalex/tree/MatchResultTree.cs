using System.Collections.Generic;

namespace CodeHive.DfaLex.tree
{
    /**
     * The result of a match operation.
     *
     * <p>A MatchResultTree contains query methods for hierarchical regex results.
     * The match boundaries, groups and group boundaries can be seen but
     * not modified through a {@code MatchResultTree}. Access is thread-safe.
     *
     * @see MatchResultTree#getRoot {@code getRoot} to get access to the hierarchy.
     */
    internal interface MatchResultTree : MatchResult
    {
        /**
	     * @return the {@link TreeNode} of the match of the whole string or throws
	     *         {@link NoSuchElementException} if no match was found.
	     */
        TreeNode getRoot();
    }

    /**
	 * A {@code TreeNode} is part of the tree structure that is a regex match.
	 * The topmost group is the whole matching string. Its children are the
	 * capture groups on the topmost layer, etc. Each {@code TreeNode} represents
	 * exactly one match for the capture group of {@link TreeNode#getGroup()}.
	 *
	 * <pre>
	 *   Regex:  "(((a+)b)+c)+"
	 *   String: aa b c aaa b c
	 *   Index:  01 2 3 456 7 8
	 *   Tree:   |/ / / |_/ / /
	 *           o / /  o  / /
	 *           |/ /   |_/ /
	 *           o /    o  /
	 *           |/     |_/
	 *           o      o
	 *           |_____/
	 *           o
	 * </pre>
	 * But groups can have different sub-groups on the same layer:
	 *
	 * <pre>
     *   Regex:  "((a+)|(b)|(c))+"
     *   String: aa b  c
     *   Index:  01 2  3
     *   Tree:   |/ |  |
     *           o  o  o
     *            \_|_/
     *              o
     * </pre>
   	 */
    public interface TreeNode
    {
        /**
		 * @return 	all sub-matches of this group. This can be different
		 * 			groups if this group contains more than one group.
		 */
        IEnumerable<TreeNode> getChildren();

        /**
		 * @return Number of the group in the original regex. This is basically n
		 * if the group is designated with the <i>n</i>th opening paren. 0 is the matching of
		 * the whole string.
		 *
		 *
		 * <pre>
		 * Regex:     ( ( a+ ) b+) (c+)
		 *            1 2          3
		 * String:    "aaabbccc"
		 * Group 0:   "aaabbccc"
		 * Group 1:   "aaabb"
		 * Group 2:   "aaa"
		 * Group 3:   "ccc"
		 * </pre>
		 */
        int getGroup();

        // TODO:
        // /** @return {@code TreeNode} of group containing this group or null if this is the root. */
        // public TreeNode getParent();
    }

    /**
     * The result of a match operation.
     *
     * <p>This interface contains query methods used to determine the
     * results of a match against a regular expression. The match boundaries,
     * groups and group boundaries can be seen but not modified through
     * a {@code MatchResult}.
     *
     * @author  Michael McCloskey
     * @see Matcher
     * @since 1.5
     */
    internal interface MatchResult
    {
        /**
         * Returns the start index of the match.
         *
         * @return  The index of the first character matched
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         */
        int start();

        /**
         * Returns the start index of the subsequence captured by the given group
         * during this match.
         *
         * <p> <a href="Pattern.html#cg">Capturing groups</a> are indexed from left
         * to right, starting at one.  Group zero denotes the entire pattern, so
         * the expression <i>m.</i>{@code start(0)} is equivalent to
         * <i>m.</i>{@code start()}.  </p>
         *
         * @param  group
         *         The index of a capturing group in this matcher's pattern
         *
         * @return  The index of the first character captured by the group,
         *          or {@code -1} if the match was successful but the group
         *          itself did not match anything
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         *
         * @throws  IndexOutOfBoundsException
         *          If there is no capturing group in the pattern
         *          with the given index
         */
        int start(int group);

        /**
         * Returns the offset after the last character matched.
         *
         * @return  The offset after the last character matched
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         */
        int end();

        /**
         * Returns the offset after the last character of the subsequence
         * captured by the given group during this match.
         *
         * <p> <a href="Pattern.html#cg">Capturing groups</a> are indexed from left
         * to right, starting at one.  Group zero denotes the entire pattern, so
         * the expression <i>m.</i>{@code end(0)} is equivalent to
         * <i>m.</i>{@code end()}.  </p>
         *
         * @param  group
         *         The index of a capturing group in this matcher's pattern
         *
         * @return  The offset after the last character captured by the group,
         *          or {@code -1} if the match was successful
         *          but the group itself did not match anything
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         *
         * @throws  IndexOutOfBoundsException
         *          If there is no capturing group in the pattern
         *          with the given index
         */
        int end(int group);

        /**
         * Returns the input subsequence matched by the previous match.
         *
         * <p> For a matcher <i>m</i> with input sequence <i>s</i>,
         * the expressions <i>m.</i>{@code group()} and
         * <i>s.</i>{@code substring(}<i>m.</i>{@code start(),}&nbsp;<i>m.</i>{@code end())}
         * are equivalent.  </p>
         *
         * <p> Note that some patterns, for example {@code a*}, match the empty
         * string.  This method will return the empty string when the pattern
         * successfully matches the empty string in the input.  </p>
         *
         * @return The (possibly empty) subsequence matched by the previous match,
         *         in string form
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         */
        string group();

        /**
         * Returns the input subsequence captured by the given group during the
         * previous match operation.
         *
         * <p> For a matcher <i>m</i>, input sequence <i>s</i>, and group index
         * <i>g</i>, the expressions <i>m.</i>{@code group(}<i>g</i>{@code )} and
         * <i>s.</i>{@code substring(}<i>m.</i>{@code start(}<i>g</i>{@code
         * ),}&nbsp;<i>m.</i>{@code end(}<i>g</i>{@code ))}
         * are equivalent.  </p>
         *
         * <p> <a href="Pattern.html#cg">Capturing groups</a> are indexed from left
         * to right, starting at one.  Group zero denotes the entire pattern, so
         * the expression {@code m.group(0)} is equivalent to {@code m.group()}.
         * </p>
         *
         * <p> If the match was successful but the group specified failed to match
         * any part of the input sequence, then {@code null} is returned. Note
         * that some groups, for example {@code (a*)}, match the empty string.
         * This method will return the empty string when such a group successfully
         * matches the empty string in the input.  </p>
         *
         * @param  group
         *         The index of a capturing group in this matcher's pattern
         *
         * @return  The (possibly empty) subsequence captured by the group
         *          during the previous match, or {@code null} if the group
         *          failed to match part of the input
         *
         * @throws  IllegalStateException
         *          If no match has yet been attempted,
         *          or if the previous match operation failed
         *
         * @throws  IndexOutOfBoundsException
         *          If there is no capturing group in the pattern
         *          with the given index
         */
        string group(int group);

        /**
         * Returns the number of capturing groups in this match result's pattern.
         *
         * <p> Group zero denotes the entire pattern by convention. It is not
         * included in this count.
         *
         * <p> Any non-negative integer smaller than or equal to the value
         * returned by this method is guaranteed to be a valid group index for
         * this matcher.  </p>
         *
         * @return The number of capturing groups in this matcher's pattern
         */
        int groupCount();
    }
}

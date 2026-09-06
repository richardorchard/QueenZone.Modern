import Svg, { G } from 'react-native-svg';
import { renderWithProviders } from '../test/render';
import { ArchiveIconPlate } from './ArchiveIconPlate';

describe('ArchiveIconPlate', () => {
  it('centres the 24-space glyph with a group transform instead of a nested Svg', () => {
    const size = 64;
    const glyphSize = Math.round(size * 0.4375);
    const s = glyphSize / 24;
    const tx = (size - glyphSize) / 2;

    const { UNSAFE_getAllByType } = renderWithProviders(
      <ArchiveIconPlate name="articles" size={size} />,
      { navigation: false },
    );

    const svgs = UNSAFE_getAllByType(Svg);
    expect(svgs).toHaveLength(1);
    expect(svgs[0].props).toMatchObject({
      width: size,
      height: size,
      viewBox: `0 0 ${size} ${size}`,
      accessibilityElementsHidden: true,
      importantForAccessibility: 'no-hide-descendants',
      'aria-hidden': true,
    });

    const groups = UNSAFE_getAllByType(G);
    expect(groups).toHaveLength(1);
    expect(groups[0].props.transform).toBe(`translate(${tx}, ${tx}) scale(${s})`);
    expect(groups[0].props.fill).toBe('none');
    expect(groups[0].props.strokeWidth).toBe(1.15);
    expect(groups[0].props.strokeLinecap).toBe('round');
    expect(groups[0].props.strokeLinejoin).toBe('round');
  });
});

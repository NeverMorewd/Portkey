import { afterNextRender, Component, effect, ElementRef, input, viewChild } from '@angular/core';
import * as echarts from 'echarts';
import type { EChartsOption } from 'echarts';

@Component({
  selector: 'app-echart',
  template: `<div #chartEl style="width:100%;height:100%"></div>`,
  styles: [':host { display:block; width:100%; height:100%; }']
})
export class EChartComponent {
  option = input.required<EChartsOption>();
  private chartEl = viewChild.required<ElementRef<HTMLDivElement>>('chartEl');
  private chart: echarts.ECharts | null = null;

  constructor() {
    afterNextRender(() => {
      this.chart = echarts.init(this.chartEl().nativeElement);
      this.chart.setOption(this.option());
    });

    effect(() => {
      const opt = this.option();
      this.chart?.setOption(opt, true);
    });
  }
}
